using System.Runtime.CompilerServices;
using System.Text;
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

    #region STREAMS (JetStream as a bounded buffer)
    public async Task PublishToStreamAsync(string streamKey, IAsyncEnumerable<DataChunk> source, CancellationToken cancellationToken = default)
    {
      var subject = NormalizeSubject(streamKey);
      await EnsureJetStreamAsync(cancellationToken);

      var streamContext = _nats.CreateJetStreamContext();
      var backpressureBudget = TimeSpan.FromSeconds(ServerCapabilities.GlobalTimeoutSeconds);
      var retryDelay = TimeSpan.FromMilliseconds(50);

      long seq = 0;
      try
      {
        await foreach (var chunk in source.WithCancellation(cancellationToken))
        {
          if (chunk is null || chunk.Length <= 0)
          {
            continue;
          }

          var payload = chunk.Data.AsMemory(0, chunk.Length);
          await PublishWithBackpressureAsync(streamContext, subject, payload, StreamHeaders.Data(seq), backpressureBudget, retryDelay, streamKey, cancellationToken);
          seq++;
        }

        // The terminal marker also flows through backpressure: the buffer may be full.
        await PublishWithBackpressureAsync(streamContext, subject, Array.Empty<byte>(), StreamHeaders.End(seq), backpressureBudget, retryDelay, streamKey, cancellationToken);
      }
      catch (Exception ex)
      {
        await TryPublishAbortAsync(streamContext, subject, streamKey, ex.Message, backpressureBudget, retryDelay);
        throw;
      }
    }

    public async IAsyncEnumerable<DataChunk> ConsumeStreamAsync(string streamKey, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
      var subject = NormalizeSubject(streamKey);
      var session = await CreateStreamConsumerAsync(streamKey, subject, cancellationToken);

      try
      {
        await foreach (var chunk in PumpOrderedChunksAsync(session, cancellationToken))
        {
          yield return chunk;
        }
      }
      finally
      {
        await CleanupStreamConsumerAsync(session.Context, session.Subject, session.ConsumerName);
      }
    }

    public async Task DeleteStreamAsync(string streamKey)
    {
      var subject = NormalizeSubject(streamKey);

      try
      {
        await EnsureJetStreamAsync();
      }
      catch
      {
        return;
      }

      var streamContext = _nats.CreateJetStreamContext();

      try { await streamContext.DeleteConsumerAsync(IntraServerStreamName, ConsumerNameFor(subject), CancellationToken.None); }
      catch { }

      await TryPurgeStreamAsync(streamContext, subject, IntraServerStreamName);
    }

    private async Task<StreamConsumeSession> CreateStreamConsumerAsync(string streamKey, string subject, CancellationToken cancellationToken)
    {
      await EnsureJetStreamAsync(cancellationToken);

      var streamContext = _nats.CreateJetStreamContext();
      var consumerName = ConsumerNameFor(subject);
      var maxInFlight = Math.Max(1, ServerCapabilities.IntraServerBufferedChunks) + 1;
      var globalTimeout = TimeSpan.FromSeconds(ServerCapabilities.GlobalTimeoutSeconds);
      var idleTimeout = TimeSpan.FromSeconds(ServerCapabilities.GlobalTimeoutSeconds * 2);

      var consumer = await streamContext.CreateOrUpdateConsumerAsync(IntraServerStreamName, new ConsumerConfig(consumerName)
      {
        FilterSubject = subject,
        AckPolicy = ConsumerConfigAckPolicy.Explicit,
        MaxAckPending = maxInFlight,
        AckWait = globalTimeout,
        InactiveThreshold = TimeSpan.FromSeconds(ServerCapabilities.GlobalTimeoutSeconds * 4),
      }, cancellationToken);

      var fetchOpts = new NatsJSFetchOpts
      {
        MaxMsgs = maxInFlight,
        Expires = globalTimeout,
      };

      return new StreamConsumeSession(streamKey, subject, streamContext, consumer, consumerName, fetchOpts, idleTimeout);
    }

    private async IAsyncEnumerable<DataChunk> PumpOrderedChunksAsync(StreamConsumeSession session, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
      while (!session.Finished)
      {
        cancellationToken.ThrowIfCancellationRequested();

        var gotMessage = false;

        await foreach (var msg in session.Consumer.FetchAsync(
          session.FetchOpts, NatsDefaultSerializer<byte[]>.Default, cancellationToken: cancellationToken))
        {
          gotMessage = true;
          session.MarkProgress();

          var outcome = await ClassifyStreamMessageAsync(msg, session, cancellationToken);
          if (outcome.Disposition == StreamMessageDisposition.EndOfStream)
          {
            break;
          }

          if (outcome.Disposition == StreamMessageDisposition.Skipped)
          {
            continue;
          }

          await msg.AckAsync(cancellationToken: cancellationToken);
          session.Advance();
          
          yield return new DataChunk(outcome.Payload!);
        }

        if (session.Finished)
        {
          yield break;
        }

        if (!gotMessage)
        {
          await WaitOrThrowOnStreamIdleAsync(session, cancellationToken);
        }
      }
    }

    private async Task<StreamMessageOutcome> ClassifyStreamMessageAsync(INatsJSMsg<byte[]> msg, StreamConsumeSession session, CancellationToken cancellationToken)
    {
      var headers = ExtractStreamHeaders(msg);
      if (!StreamHeaders.TryParse(headers, out var parsed))
      {
        await msg.AckAsync(cancellationToken: cancellationToken);
        throw new InvalidOperationException(
          $"Stream '{session.StreamKey}' received a message without valid stream headers.");
      }

      var payload = msg.Data;

      if (parsed.Kind == StreamHeaders.ErrorKind)
      {
        await msg.AckAsync(cancellationToken: cancellationToken);
        var reason = payload == null ? "" : Encoding.UTF8.GetString(payload);
        throw new InvalidOperationException(string.IsNullOrEmpty(reason) ? $"Remote stream '{session.StreamKey}' failed." : reason);
      }

      if (parsed.Kind == StreamHeaders.EndKind)
      {
        await CompleteStreamOnEndMarkerAsync(msg, session, parsed, cancellationToken);
        return StreamMessageOutcome.Ended;
      }

      if (parsed.Kind != StreamHeaders.DataKind)
      {
        await DiscardUnknownStreamKindAsync(msg, cancellationToken);
        return StreamMessageOutcome.Skipped;
      }

      if (await DiscardStaleRedeliveryAsync(msg, session, parsed, cancellationToken))
      {
        return StreamMessageOutcome.Skipped;
      }

      ThrowOnSequenceGap(session, parsed);

      if (await DiscardEmptyPayloadAsync(msg, session, cancellationToken))
      {
        return StreamMessageOutcome.Skipped;
      }

      return StreamMessageOutcome.Data(payload);
    }

    private static NatsHeaders? ExtractStreamHeaders(INatsJSMsg<byte[]> msg)
    {
      // Headers live on the concrete message type, not the interface.
      return msg is NatsJSMsg<byte[]> concrete ? concrete.Headers : null;
    }

    private async Task CompleteStreamOnEndMarkerAsync(INatsJSMsg<byte[]> msg, StreamConsumeSession session, StreamHeaders.Parsed parsed, CancellationToken cancellationToken)
    {
      await msg.AckAsync(cancellationToken: cancellationToken);
      if (parsed.FinalSeq != session.Expected)
      {
        throw new InvalidOperationException(
          $"Stream '{session.StreamKey}' is incomplete: received {session.Expected} of {parsed.FinalSeq} chunks.");
      }
      session.Complete();
    }

    private static async Task DiscardUnknownStreamKindAsync(INatsJSMsg<byte[]> msg, CancellationToken cancellationToken)
    {
      await msg.AckAsync(cancellationToken: cancellationToken);
    }

    private static async Task<bool> DiscardStaleRedeliveryAsync(INatsJSMsg<byte[]> msg, StreamConsumeSession session, StreamHeaders.Parsed parsed, CancellationToken cancellationToken)
    {
      if (parsed.Seq >= session.Expected)
      {
        return false;
      }

      // At-least-once redelivery (AckWait fired while downstream was slow):
      // already yielded, just ack and skip.
      await msg.AckAsync(cancellationToken: cancellationToken);
      return true;
    }

    private static void ThrowOnSequenceGap(StreamConsumeSession session, StreamHeaders.Parsed parsed)
    {
      if (parsed.Seq > session.Expected)
      {
        throw new InvalidOperationException(
          $"Stream '{session.StreamKey}' lost data: expected chunk {session.Expected} but got {parsed.Seq}.");
      }
    }

    private static async Task<bool> DiscardEmptyPayloadAsync(INatsJSMsg<byte[]> msg, StreamConsumeSession session, CancellationToken cancellationToken)
    {
      var payload = msg.Data;
      if (payload != null && payload.Length != 0)
      {
        return false;
      }

      await msg.AckAsync(cancellationToken: cancellationToken);
      session.Advance();
      return true;
    }

    private static async Task WaitOrThrowOnStreamIdleAsync(StreamConsumeSession session, CancellationToken cancellationToken)
    {
      if (DateTime.UtcNow - session.LastProgress >= session.IdleTimeout)
      {
        throw new TimeoutException(
          $"Timed out waiting for stream '{session.StreamKey}' after {session.Expected} chunks. The writer stalled or crashed.");
      }
      await Task.Delay(50, cancellationToken);
    }

    private async Task CleanupStreamConsumerAsync(INatsJSContext streamContext, string subject, string consumerName)
    {
      // Delete the consumer first so no further delivery/redelivery happens,
      // then purge leftovers. Purging first can delete un-acked messages
      // (including End) that an in-flight Fetch still expects.
      try { await streamContext.DeleteConsumerAsync(IntraServerStreamName, consumerName, CancellationToken.None); }
      catch (Exception ex) { _logger?.LogDebug(ex, "Failed to delete consumer '{Consumer}'", consumerName); }

      await TryPurgeStreamAsync(streamContext, subject, IntraServerStreamName);
    }

    private async Task PublishWithBackpressureAsync(
      INatsJSContext streamContext,
      string subject,
      ReadOnlyMemory<byte> payload,
      NatsHeaders headers,
      TimeSpan budget,
      TimeSpan retryDelay,
      string streamKey,
      CancellationToken cancellationToken)
    {
      var deadline = DateTime.UtcNow + budget;
      var loggedWait = false;
      var attempt = 0;
      var maxDelay = TimeSpan.FromMilliseconds(500);

      while (true)
      {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
          var ack = await streamContext.PublishAsync(subject, payload, NatsDefaultSerializer<ReadOnlyMemory<byte>>.Default, headers: headers, cancellationToken: cancellationToken);
          ack.EnsureSuccess();
          return;
        }
        catch (OperationCanceledException)
        {
          throw;
        }
        catch (Exception ex) when (IsBackpressureRejection(ex))
        {
          if (DateTime.UtcNow >= deadline)
          {
            throw new TimeoutException(
              $"Timed out publishing to stream '{streamKey}': buffer full " +
              $"({ServerCapabilities.IntraServerBufferedChunks} chunks in flight). " +
              "The reader is too slow or not consuming.");
          }

          if (!loggedWait)
          {
            _logger?.LogDebug("Backpressure on stream '{StreamKey}', waiting for buffer space", streamKey);
            loggedWait = true;
          }

          await Task.Delay(ComputeBackpressureDelay(retryDelay, maxDelay, attempt++), cancellationToken);
        }
      }
    }

    private async Task TryPublishAbortAsync(
      INatsJSContext streamContext,
      string subject,
      string streamKey,
      string reason,
      TimeSpan budget,
      TimeSpan retryDelay)
    {
      await TryPurgeStreamAsync(streamContext, subject, IntraServerStreamName);

      try
      {
        var abort = Encoding.UTF8.GetBytes(reason);
        await PublishWithBackpressureAsync(streamContext, subject, abort, StreamHeaders.Error(), budget, retryDelay, streamKey, CancellationToken.None);
      }
      catch (Exception ex)
      {
        _logger?.LogWarning(ex, "Failed to publish abort marker for stream '{StreamKey}'", streamKey);
      }
    }

    private async Task TryPurgeStreamAsync(INatsJSContext streamContext, string subject, string streamKey)
    {
      try { await streamContext.PurgeStreamAsync(streamKey, new StreamPurgeRequest { Filter = subject }, CancellationToken.None); }
      catch (Exception ex) { _logger?.LogWarning(ex, "Failed to purge stream '{StreamKey}'", streamKey); }
    }

    private static bool IsBackpressureRejection(Exception ex)
    {
      for (var current = ex; current != null; current = current.InnerException)
      {
        if (current is NatsJSApiException apiEx && apiEx.Error != null)
        {
          return apiEx.Error.Description?.Contains("maximum messages", StringComparison.OrdinalIgnoreCase) == true;
        }
      }

      // Fallback for untyped/wrapped variants across server versions.
      return ex.Message.Contains("maximum messages", StringComparison.OrdinalIgnoreCase);
    }

    private static TimeSpan ComputeBackpressureDelay(TimeSpan initial, TimeSpan max, int attempt)
    {
      var grownMs = initial.TotalMilliseconds * (1 << Math.Min(attempt, 5));
      var cappedMs = Math.Min(grownMs, max.TotalMilliseconds);
      // +/-20% jitter to avoid lockstep retries from concurrent producers.
      var jitterMs = cappedMs * 0.2 * (Random.Shared.NextDouble() * 2 - 1);
      return TimeSpan.FromMilliseconds(Math.Max(1, cappedMs + jitterMs));
    }

    private async Task EnsureJetStreamAsync(CancellationToken cancellationToken = default)
    {
      if (_jetStreamReady) return;

      await _connectLock.WaitAsync(cancellationToken);
      try
      {
        if (_jetStreamReady) return;

        var streamContext = _nats.CreateJetStreamContext();

        var maxPerSubject = Math.Max(1, ServerCapabilities.IntraServerBufferedChunks) + 1;

        await streamContext.CreateOrUpdateStreamAsync(new StreamConfig(IntraServerStreamName, [IntraServerSubjectPrefix + ".>"])
        {
          Retention = StreamConfigRetention.Workqueue,
          Storage = StreamConfigStorage.Memory, // TO DO: consider disk if we want to survive a server crash / minimize memory pressure
          AllowDirect = true,
          MaxMsgsPerSubject = maxPerSubject,
          Discard = StreamConfigDiscard.New,
          DiscardNewPerSubject = true,
          MaxAge = TimeSpan.FromSeconds(ServerCapabilities.GlobalTimeoutSeconds * 4),
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

    #endregion

    public async ValueTask DisposeAsync()
    {
      await _nats.DisposeAsync();
      _connectLock.Dispose();
    }
  }
}
