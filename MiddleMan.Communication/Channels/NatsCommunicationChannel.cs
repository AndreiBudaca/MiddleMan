using System.Runtime.CompilerServices;
using System.Text.Json;
using MiddleMan.Core;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Net;
using MiddleMan.Communication.Channels.Classes;
using MiddleMan.Communication.Channels.Serialization;

namespace MiddleMan.Communication.Channels
{
  public sealed class NatsCommunicationChannel : ICommunicationChannel
  {
    private const string IntraServerStreamName = "INTRA_SERVER";
    private const string IntraServerSubjectPrefix = "intra-server";

    private readonly NatsConnection _nats;
    private readonly ILogger<NatsCommunicationChannel>? _logger;
    private volatile bool _jetStreamReady;
    private readonly SemaphoreSlim _connectLock = new(1, 1);

    public NatsCommunicationChannel(string connectionString, ILogger<NatsCommunicationChannel>? logger = null)
    {
      var opts = new NatsOpts
      {
        Url = connectionString,
        RequestTimeout = TimeSpan.FromSeconds(ServerCapabilities.ClientConnectionTimeoutSeconds),
        RequestReplyMode = NatsRequestReplyMode.SharedInbox,
        SerializerRegistry = new NatsJsonContextSerializerRegistry(MiddleManNatsJsonContext.Default),
      };
      _nats = new NatsConnection(opts);
      _logger = logger;
    }

    #region REQUEST / REPLY
     private static readonly NatsSubOpts _requestSubscriptionOpts = new()
    {
      ThrowIfNoResponders = false,
    };

    private static readonly TimeSpan _requestResubscribeDelay = TimeSpan.FromSeconds(5);

    public async Task<TReply?> RequestAsync<TRequest, TReply>(string subject, TRequest request, CancellationToken cancellationToken = default)
    {
      var reply = await _nats.RequestAsync<TRequest, TReply>(subject, request, replyOpts: _requestSubscriptionOpts, cancellationToken: cancellationToken);

      if (reply.HasNoResponders)
      {
        return default;
      }

      return reply.Data;
    }

    public async Task<IAsyncDisposable> SubscribeRequestAsync<TRequest, TReply>(string subject, Func<TRequest, TReply?> handler)
    {
      var cts = new CancellationTokenSource();

      var loop = Task.Run(() => RunRequestLoopAsync(subject, handler, cts), CancellationToken.None);

      return new RequestSubscription(cts, loop);
    }

    private async Task RunRequestLoopAsync<TRequest, TReply>(string subject, Func<TRequest, TReply?> handler, CancellationTokenSource cts)
    {
      while (!cts.IsCancellationRequested)
      {
        try
        {
          await foreach (var msg in _nats.SubscribeAsync<TRequest>(subject, cancellationToken: cts.Token))
          {
            TReply? reply;
            try
            {
              if (msg.Data == null) continue;

              reply = handler(msg.Data);
              if (reply == null) continue;
            }
            catch (Exception ex)
            {
              _logger?.LogError(ex, "Error handling request for subject '{Subject}'", subject);
              continue;
            }

            try
            {
              // Subscription lifetime must not cancel in-flight replies.
              await msg.ReplyAsync(reply, cancellationToken: CancellationToken.None);
            }
            catch (Exception ex)
            {
              _logger?.LogWarning(ex, "Failed to publish reply for subject '{Subject}'", subject);
            }
          }

          cts.Token.ThrowIfCancellationRequested();

          // The subscription ended without cancellation (e.g. transient NATS failure
          // surfaced as graceful completion). Fall through to the retry below.
          _logger?.LogWarning("Request subscription for subject '{Subject}' ended unexpectedly, resubscribing", subject);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
          break;
        }
        catch (ObjectDisposedException) when (cts.IsCancellationRequested)
        {
          break;
        }
        catch (Exception ex)
        {
          _logger?.LogWarning(ex, "Request subscription for subject '{Subject}' failed, resubscribing", subject);
        }

        try
        {
          await Task.Delay(_requestResubscribeDelay, cts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
          break;
        }
        catch (ObjectDisposedException) when (cts.IsCancellationRequested)
        {
          break;
        }
      }
    }

    #endregion

    #region STREAMS (JetStream, backpressure hidden inside the channel)

    public async Task PublishToStreamAsync(string streamKey, IAsyncEnumerable<DataChunk> source, CancellationToken cancellationToken = default)
    {
      var subject = NormalizeSubject(streamKey);
      var creditSubject = CreditSubjectFor(subject);
      await EnsureJetStreamAsync(cancellationToken);
      var js = _nats.CreateJetStreamContext();
      var creditTimeout = TimeSpan.FromSeconds(ServerCapabilities.GlobalTimeoutSeconds);

      var creditConsumer = await js.CreateOrUpdateConsumerAsync(IntraServerStreamName, new ConsumerConfig(CreditConsumerNameFor(subject))
      {
        FilterSubject = creditSubject,
        AckPolicy = ConsumerConfigAckPolicy.Explicit,
      }, cancellationToken);

      try
      {
        // Initial credit published by the reader; JetStream retains it so start order doesn't matter.
        var permits = await TakeCreditAsync(creditConsumer, creditTimeout, streamKey, cancellationToken);

        await foreach (var chunk in source.WithCancellation(cancellationToken))
        {
          while (permits <= 0)
          {
            permits += await TakeCreditAsync(creditConsumer, creditTimeout, streamKey, cancellationToken);
          }

          permits--;
          var payload = chunk.Data.AsSpan(0, chunk.Length).ToArray();
          await js.PublishAsync(subject, JsonSerializer.SerializeToUtf8Bytes(StreamEnvelope.Data(payload, chunk.Length)), cancellationToken: cancellationToken);
        }

        // Terminal markers bypass credit: the reader needs End to finish, and a failed
        // writer must still deliver Error even when no credit is available.
        await js.PublishAsync(subject, JsonSerializer.SerializeToUtf8Bytes(StreamEnvelope.End()), cancellationToken: cancellationToken);
      }
      catch (Exception ex)
      {
        // Best-effort terminal error marker so a waiting reader surfaces the failure.
        try { await js.PublishAsync(subject, JsonSerializer.SerializeToUtf8Bytes(StreamEnvelope.FromError(ex.Message)), cancellationToken: CancellationToken.None); }
        catch { }
        throw;
      }
      finally
      {
        try { await js.DeleteConsumerAsync(IntraServerStreamName, CreditConsumerNameFor(subject), CancellationToken.None); }
        catch { }
      }
    }

    public async IAsyncEnumerable<DataChunk> ConsumeStreamAsync(string streamKey, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
      var subject = NormalizeSubject(streamKey);
      var creditSubject = CreditSubjectFor(subject);
      await EnsureJetStreamAsync(cancellationToken);
      var js = _nats.CreateJetStreamContext();
      var consumerName = ConsumerNameFor(subject);
      var bufferedChunks = Math.Max(1, ServerCapabilities.IntraServerBufferedChunks);

      var consumer = await js.CreateOrUpdateConsumerAsync(IntraServerStreamName, new ConsumerConfig(consumerName)
      {
        FilterSubject = subject,
        AckPolicy = ConsumerConfigAckPolicy.Explicit,
        MaxAckPending = bufferedChunks,
        AckWait = TimeSpan.FromSeconds(ServerCapabilities.GlobalTimeoutSeconds),
      }, cancellationToken);

      // Initial credit: writer cannot publish data until the reader is ready.
      // Stored in JetStream, so a writer that started first still observes it.
      await js.PublishAsync(creditSubject, JsonSerializer.SerializeToUtf8Bytes(new CreditEnvelope { Count = bufferedChunks }), cancellationToken: cancellationToken);

      var fetchOpts = new NatsJSFetchOpts
      {
        MaxMsgs = bufferedChunks,
        Expires = TimeSpan.FromSeconds(ServerCapabilities.GlobalTimeoutSeconds),
      };

      try
      {
        while (!cancellationToken.IsCancellationRequested)
        {
          var completedBatch = true;

          await foreach (var msg in consumer.FetchAsync<byte[]>(fetchOpts, cancellationToken: cancellationToken))
          {
            completedBatch = false;

            StreamEnvelope? envelope = null;
            try { envelope = JsonSerializer.Deserialize<StreamEnvelope>(msg.Data); }
            catch { }
            if (envelope == null)
            {
              await msg.AckAsync(cancellationToken: cancellationToken);
              continue;
            }

            if (envelope.Kind == StreamEnvelope.EndKind)
            {
              await msg.AckAsync(cancellationToken: cancellationToken);
              yield break;
            }

            if (envelope.Kind == StreamEnvelope.ErrorKind)
            {
              await msg.AckAsync(cancellationToken: cancellationToken);
              throw new InvalidOperationException(envelope.Error ?? $"Remote stream '{streamKey}' failed.");
            }

            if (envelope.Kind != StreamEnvelope.DataKind || envelope.Payload == null)
            {
              await msg.AckAsync(cancellationToken: cancellationToken);
              continue;
            }

            // Ack before yielding, then hand one credit back so the writer can
            // publish the next chunk. This keeps at most `bufferedChunks`
            // data messages outstanding, hiding backpressure from callers.
            await msg.AckAsync(cancellationToken: cancellationToken);
            await js.PublishAsync(creditSubject, JsonSerializer.SerializeToUtf8Bytes(new CreditEnvelope { Count = 1 }), cancellationToken: cancellationToken);
            yield return new DataChunk(envelope.Payload, envelope.Length);
          }

          if (completedBatch)
          {
            // Fetch expired without messages: writer may not have published yet.
            // Yield control briefly before polling again.
            await Task.Delay(50, cancellationToken);
          }
        }
      }
      finally
      {
        try { await js.DeleteConsumerAsync(IntraServerStreamName, consumerName, CancellationToken.None); }
        catch { }
      }
    }

    public async Task DeleteStreamAsync(string streamKey)
    {
      var subject = NormalizeSubject(streamKey);
      var creditSubject = CreditSubjectFor(subject);

      try
      {
        await EnsureJetStreamAsync();
      }
      catch
      {
        return;
      }

      var js = _nats.CreateJetStreamContext();

      try { await js.DeleteConsumerAsync(IntraServerStreamName, ConsumerNameFor(subject), CancellationToken.None); }
      catch { }

      try { await js.DeleteConsumerAsync(IntraServerStreamName, CreditConsumerNameFor(subject), CancellationToken.None); }
      catch { }

      try { await js.PurgeStreamAsync(IntraServerStreamName, new StreamPurgeRequest { Filter = subject }, CancellationToken.None); }
      catch { }

      try { await js.PurgeStreamAsync(IntraServerStreamName, new StreamPurgeRequest { Filter = creditSubject }, CancellationToken.None); }
      catch { }
    }

    private static async Task<int> TakeCreditAsync(INatsJSConsumer creditConsumer, TimeSpan timeout, string streamKey, CancellationToken cancellationToken)
    {
      await foreach (var msg in creditConsumer.FetchAsync<byte[]>(new NatsJSFetchOpts { MaxMsgs = 1, Expires = timeout }, cancellationToken: cancellationToken))
      {
        CreditEnvelope? credit = null;
        try { credit = JsonSerializer.Deserialize<CreditEnvelope>(msg.Data); }
        catch { }

        await msg.AckAsync(cancellationToken: cancellationToken);

        if (credit is { Count: > 0 })
        {
          return credit.Count;
        }
      }

      throw new TimeoutException($"Timed out waiting for stream credit on '{streamKey}'. The reader is not consuming.");
    }

    private async Task EnsureJetStreamAsync(CancellationToken cancellationToken = default)
    {
      if (_jetStreamReady) return;

      await _connectLock.WaitAsync(cancellationToken);
      try
      {
        if (_jetStreamReady) return;

        var js = _nats.CreateJetStreamContext();

        await js.CreateOrUpdateStreamAsync(new StreamConfig(IntraServerStreamName, [IntraServerSubjectPrefix + ".>"])
        {
          Retention = StreamConfigRetention.Workqueue,
          Storage = StreamConfigStorage.Memory,
          AllowDirect = true,
        }, cancellationToken);
        _jetStreamReady = true;
      }
      finally
      {
        _connectLock.Release();
      }
    }

    private static string NormalizeSubject(string streamKey) => streamKey.Replace(':', '.').Replace(' ', '_');

    private static string ConsumerNameFor(string subject) => subject.Replace('.', '-').Replace(':', '-');

    private static string CreditSubjectFor(string subject) => subject + ".credit";

    private static string CreditConsumerNameFor(string subject) => CreditSubjectFor(subject).Replace('.', '-').Replace(':', '-');

    #endregion

    public async ValueTask DisposeAsync()
    {
      await _nats.DisposeAsync();
      _connectLock.Dispose();
    }
  }
}
