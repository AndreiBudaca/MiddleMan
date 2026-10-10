using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using MiddleMan.Core;

namespace MiddleMan.Web.Communication.CommunicationSync.SameServer
{
  public static class StreamingCommunicationTaskRegistry
  {
    public sealed record TrackedCommunication(
      Guid Correlation,
      Task Task,
      CancellationTokenSource Cts,
      string ConnectionId);

    private static readonly ConcurrentDictionary<Guid, TrackedCommunication> tasks = new();

    public static TrackedCommunication Track(Guid correlation, Task task, CancellationTokenSource cts, string connectionId)
    {
      var tracked = new TrackedCommunication(correlation, task, cts, connectionId);
      tasks[correlation] = tracked;
      return tracked;
    }

    public static TrackedCommunication? TryGet(Guid correlation)
      => tasks.TryGetValue(correlation, out var tracked) ? tracked : null;

    public static TrackedCommunication? Take(Guid correlation)
      => tasks.TryRemove(correlation, out var tracked) ? tracked : null;

    // Starts the inter-replica bridge on a different thread than the SignalR
    // client stream so SubscribeToServer can yield download chunks promptly.
    // Never await the returned full task inside the SubscribeToServer iterator
    // (not even in finally): SignalR sends EOS only after the iterator -
    // including finally - finishes, but the task needs the client upload EOS
    // (AddReadChannel) which only arrives after the client sees EOS.
    // The task is awaited later in AddReadChannel, never in the stream.
    public static TrackedCommunication StartBridge(
      Guid correlation,
      string connectionId,
      CancellationToken connectionAborted,
      Func<CancellationToken, Task> factory,
      ILogger logger)
    {
      var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(connectionAborted);
      var linkedToken = linkedCts.Token;

      // TO DO: Use custom exceptions. CancellationToken.None so cancellation only
      // flows into the bridge via linkedToken instead of preventing start.
      var task = Task.Run(() => factory(linkedToken), CancellationToken.None);
      var tracked = Track(correlation, task, linkedCts, connectionId);

      // Observe failures for cases where AddReadChannel never arrives
      // (e.g. client disconnects mid-download).
      _ = task.ContinueWith(
        t =>
        {
          if (t.IsFaulted && t.Exception != null)
          {
            logger.LogError(t.Exception, "Streaming communication {Correlation} failed. Connection ID: {ConnectionId}", correlation, connectionId);
          }
        },
        CancellationToken.None,
        TaskContinuationOptions.OnlyOnFaulted,
        TaskScheduler.Default);

      return tracked;
    }

    // Phase-1 join only: waits for NATS input to be fully forwarded to the
    // channel writer before EOS. Does NOT wait for the client upload
    // (AddReadChannel) or the NATS response publish.
    public static async Task WaitForDownloadAsync(
      TrackedCommunication tracked,
      Task writerCompletion,
      ChannelWriter<DataChunk> channelWriter,
      TimeSpan timeout,
      ILogger logger,
      CancellationToken connectionAborted)
    {
      try
      {
        await writerCompletion.WaitAsync(timeout, connectionAborted);
      }
      catch (OperationCanceledException) when (connectionAborted.IsCancellationRequested)
      {
        throw;
      }
      catch (OperationCanceledException ex)
      {
        // WriterCompletion was canceled via TryCancelAll (bridge failure).
        channelWriter.TryComplete(ex);
        throw new HubException("Streaming communication failed while receiving server data.");
      }
      catch (TimeoutException ex)
      {
        // Surface the bridge download error instead of a generic timeout when available.
        if (tracked.Task.IsFaulted && tracked.Task.Exception != null)
        {
          logger.LogError(tracked.Task.Exception, "Streaming communication {Correlation} failed before WriterCompletion. Connection ID: {ConnectionId}", tracked.Correlation, tracked.ConnectionId);
        }
        channelWriter.TryComplete(ex);
        throw new HubException("Timed out waiting for server data.");
      }
    }

    // AddReadChannel path: the full bridge task has settled, so dispose now.
    public static async Task ReleaseAfterUploadAsync(Guid correlation, CancellationToken connectionAborted)
    {
      if (!tasks.TryGetValue(correlation, out var tracked)) return;

      try
      {
        await tracked.Task.WaitAsync(connectionAborted);
      }
      finally
      {
        // Only the remover disposes; disconnect/failure paths use deferred dispose.
        if (tasks.TryRemove(correlation, out var removed))
        {
          removed.Cts.Dispose();
        }
      }
    }

    // SubscribeToServer failure path: unblock the bridge without awaiting it.
    public static void AbortDownload(Guid correlation, ILogger logger)
    {
      var removed = Take(correlation);
      if (removed == null) return;

      CancelAndDisposeDeferred(removed, logger);
    }

    // OnDisconnectedAsync path: never blocks on the bridge tasks.
    public static void AbortConnection(string connectionId, ILogger logger)
    {
      foreach (var tracked in TakeByConnectionId(connectionId))
      {
        // Unblock the bridge task (NATS read / TCS waits). The task itself
        // performs RemoveSession(cancel: true) on failure.
        CancelAndDisposeDeferred(tracked, logger, logAsDisconnect: true);
      }
    }

    private static void CancelAndDisposeDeferred(TrackedCommunication tracked, ILogger logger, bool logAsDisconnect = false)
    {
      try
      {
        tracked.Cts.Cancel();
      }
      catch (Exception ex)
      {
        logger.LogWarning(ex, "Failed to cancel streaming communication {Correlation}. Connection ID: {ConnectionId}", tracked.Correlation, tracked.ConnectionId);
      }
      finally
      {
        // Dispose once the task settles; never block the caller on it.
        _ = tracked.Task.ContinueWith(
          t =>
          {
            if (t.IsFaulted && t.Exception != null)
            {
              if (logAsDisconnect)
              {
                logger.LogWarning(t.Exception, "Streaming communication {Correlation} ended with error after disconnect. Connection ID: {ConnectionId}", tracked.Correlation, tracked.ConnectionId);
              }
              else
              {
                logger.LogWarning(t.Exception, "Streaming communication {Correlation} ended with error after abort. Connection ID: {ConnectionId}", tracked.Correlation, tracked.ConnectionId);
              }
            }
            tracked.Cts.Dispose();
          },
          CancellationToken.None,
          TaskContinuationOptions.ExecuteSynchronously,
          TaskScheduler.Default);
      }
    }

    private static IReadOnlyList<TrackedCommunication> TakeByConnectionId(string connectionId)
    {
      var removed = new List<TrackedCommunication>();
      foreach (var kvp in tasks.ToArray())
      {
        if (kvp.Value.ConnectionId == connectionId && tasks.TryRemove(kvp.Key, out var tracked))
        {
          removed.Add(tracked);
        }
      }
      return removed;
    }
  }
}
