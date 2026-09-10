using System.Collections.Concurrent;
using MiddleMan.Core;
using MiddleMan.Core.Extensions;
using NATS.Client.Core;

namespace MiddleMan.Communication.Channels
{
  public sealed class NatsCommunicationChannel : ICommunicationChannel
  {
    private readonly NatsConnection _nats;
    private readonly ConcurrentDictionary<string, List<CancellationTokenSource>> _subscriptions = new();
    private readonly ConcurrentDictionary<string, List<Task>> _subscriptionTasks = new();
    private volatile bool _connected;
    private readonly SemaphoreSlim _connectLock = new(1, 1);

    public NatsCommunicationChannel(string connectionString)
    {
      var opts = new NatsOpts
      {
        Url = connectionString,
        RequestTimeout = TimeSpan.FromSeconds(ServerCapabilities.ClientConnectionTimeoutSeconds),
        RequestReplyMode = NatsRequestReplyMode.SharedInbox,
      };
      _nats = new NatsConnection(opts);
    }

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken = default)
    {
      if (_connected && _nats.ConnectionState == NatsConnectionState.Open) return;

      await _connectLock.WaitAsync(cancellationToken);
      try
      {
        if (_connected && _nats.ConnectionState == NatsConnectionState.Open) return;

        await _nats.ConnectAsync();
        _connected = true;
      }
      finally
      {
        _connectLock.Release();
      }
    }

    #region PUB / SUB
    public async Task PublishAsync<T>(string subject, T message, CancellationToken cancellationToken = default)
    {
      await EnsureConnectedAsync(cancellationToken);
      await _nats.PublishAsync(subject, message, cancellationToken: cancellationToken);
    }

    public async Task SubscribeAsync<T>(string topic, Func<T, Task> onMessageReceived, CancellationToken cancellationToken = default)
    {
      await EnsureConnectedAsync(cancellationToken);

      var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

      _subscriptions.AddOrUpdate(topic, cts);

      var task = Task.Run(async () =>
      {
        try
        {
          await foreach (var msg in _nats.SubscribeAsync<T>(topic, cancellationToken: cts.Token))
          {
            if (msg.Data == null) continue;
            await onMessageReceived(msg.Data);
          }
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested) { }
      }, cts.Token);

      _subscriptionTasks.AddOrUpdate(topic, task);
    }

    public async Task UnsubscribeAsync(string topic)
    {
      if (_subscriptions.TryRemove(topic, out var ctsList))
      {
        foreach (var cts in ctsList)
        {
          cts.Cancel();
          cts.Dispose();
        }
      }

      if (_subscriptionTasks.TryRemove(topic, out var tasks))
      {
        try { await Task.WhenAll(tasks); } catch { }
      }
    }

    #endregion

    #region REQUEST / REPLY

    public async Task<TReply?> RequestAsync<TRequest, TReply>(string subject, TRequest request, CancellationToken cancellationToken = default)
    {
      await EnsureConnectedAsync(cancellationToken);

      var replyOpts = new NatsSubOpts
      {
        Timeout = TimeSpan.FromSeconds(ServerCapabilities.ClientConnectionTimeoutSeconds),
        ThrowIfNoResponders = true
      };

      try
      {
        var reply = await _nats.RequestAsync<TRequest, TReply>(subject, request, replyOpts: replyOpts, cancellationToken: cancellationToken);

        return reply.Data;
      }
      catch (Exception ex) when (ex is NatsNoRespondersException || ex is NatsTimeoutException)
      {
        return default;
      }
    }

    public async Task SubscribeRequestAsync<TRequest, TReply>(string subject, Func<TRequest, Task<TReply?>> handler, CancellationToken cancellationToken = default)
    {
      await EnsureConnectedAsync(cancellationToken);

      var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
      _subscriptions.AddOrUpdate(subject, cts);

      var task = Task.Run(async () =>
      {
        try
        {
          await foreach (var msg in _nats.SubscribeAsync<TRequest>(subject, cancellationToken: cts.Token))
          {
            try
            {
              if (msg.Data == null) continue;

              var reply = await handler(msg.Data);
              if (reply == null) continue;

              await msg.ReplyAsync(reply, cancellationToken: cts.Token);
            }
            catch (Exception ex)
            {
              // TODO: Replace with logger
              Console.WriteLine($"Error handling request for subject '{subject}': {ex}");
            }
          }
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested) { }
      }, cts.Token);

      _subscriptionTasks.AddOrUpdate(subject, task);
    }

    #endregion

    #region STREAMS - NotImplemented

    public Task CreateStreamAsync(string streamKey) => throw new NotImplementedException("Streams not yet migrated to NATS JetStream");

    public Task AddToStreamAsync(string streamKey, DataChunk data) => throw new NotImplementedException("Streams not yet migrated to NATS JetStream");

    public Task TerminateWithErrorAsync(string streamKey, string errorMessage) => throw new NotImplementedException("Streams not yet migrated to NATS JetStream");

    public Task SignalStreamEndAsync(string streamKey) => throw new NotImplementedException("Streams not yet migrated to NATS JetStream");

    public IAsyncEnumerable<DataChunk> ConsumeStreamAsync(string streamKey, CancellationToken cancellationToken) => throw new NotImplementedException("Streams not yet migrated to NATS JetStream");

    public Task DeleteStreamAsync(string streamKey) => throw new NotImplementedException("Streams not yet migrated to NATS JetStream");

    #endregion

    public async ValueTask DisposeAsync()
    {
      foreach (var kv in _subscriptions)
      {
        foreach (var cts in kv.Value)
        {
          cts.Cancel();
          cts.Dispose();
        }
      }
      _subscriptions.Clear();

      foreach (var kv in _subscriptionTasks)
      {
        try { await Task.WhenAll(kv.Value); } catch { }
      }
      _subscriptionTasks.Clear();

      await _nats.DisposeAsync();
      _connectLock.Dispose();
    }
  }
}
