using System.Runtime.CompilerServices;
using MiddleMan.Communication.Adapters;
using MiddleMan.Communication.Channels;
using MiddleMan.Communication.SyncMechanisms;
using MiddleMan.Core;
using MiddleMan.Data.InMemory;

namespace MiddleMan.Communication;

public class IntraServerCommunicationManager(ICommunicationChannel communicationChannel, IInMemoryContext inMemoryContext)
{
  private readonly static AsyncResourceMonitor<Guid> sessionMonitors = new();
  private readonly ICommunicationChannel communicationChannel = communicationChannel;
  private readonly IInMemoryContext inMemoryContext = inMemoryContext;

  public async Task RegisterRequestSession(Guid correlation, bool sameServer)
  {
    inMemoryContext.AddToHash("intraServerRequestSessions", correlation.ToString(), true);

    if (!sameServer)
    {
      await RegisterTokens(RequestChannelTokenKey(correlation), correlation);
    }
  }

  public async Task RegisterResponseSession(Guid correlation)
  {
    await RegisterTokens(ResponseChannelTokenKey(correlation), correlation);
  }

  public async Task ClearRequestSession(Guid correlation)
  {
    inMemoryContext.RemoveFromHash("intraServerRequestSessions", correlation.ToString());
    inMemoryContext.RemoveList(RequestChannelTokenKey(correlation));

    await communicationChannel.UnsubscribeAsync(RequestChannelTokenKey(correlation));
    await communicationChannel.DeleteKeyAsync(RequestChannelChunksKey(correlation));
    await communicationChannel.DeleteKeyAsync(ResponseChannelChunksKey(correlation));
  }

  public async Task ClearResponseSession(Guid correlation)
  {
    inMemoryContext.RemoveList(ResponseChannelTokenKey(correlation));
    await communicationChannel.UnsubscribeAsync(ResponseChannelTokenKey(correlation));
  }

  public bool ExistsRequestSession(Guid correlation)
  {
    return inMemoryContext.ExistsInHash("intraServerRequestSessions", correlation.ToString());
  }

  public Task WriteRequestAsync(IDataWriterAdapter dataWriterAdapter, Guid correlation, CancellationToken cancellationToken = default)
  {
    return WriteRequestAsync(dataWriterAdapter.Adapt(), correlation, cancellationToken);
  }

  public Task WriteRequestAsync(IAsyncEnumerable<byte[]> dataSource, Guid correlation, CancellationToken cancellationToken = default)
  {
    return WriteAsync(dataSource, RequestChannelChunksKey(correlation), RequestChannelTokenKey(correlation), correlation, cancellationToken);
  }

  public Task WriteResponseAsync(IDataWriterAdapter dataWriterAdapter, Guid correlation, CancellationToken cancellationToken = default)
  {
    return WriteResponseAsync(dataWriterAdapter.Adapt(), correlation, cancellationToken);
  }

  public Task WriteResponseAsync(IAsyncEnumerable<byte[]> dataSource, Guid correlation, CancellationToken cancellationToken = default)
  {
    return WriteAsync(dataSource, ResponseChannelChunksKey(correlation), ResponseChannelTokenKey(correlation), correlation, cancellationToken);
  }

  public IAsyncEnumerable<byte[]> ReadRequestAsync(Guid correlation, CancellationToken cancellationToken = default)
  {
    return ReadAsync(RequestChannelChunksKey(correlation), RequestChannelTokenKey(correlation), cancellationToken);
  }

  public IAsyncEnumerable<byte[]> ReadResponseAsync(Guid correlation, CancellationToken cancellationToken = default)
  {
    return ReadAsync(ResponseChannelChunksKey(correlation), ResponseChannelTokenKey(correlation), cancellationToken);
  }

  private async Task RegisterTokens(string tokensKey, Guid correlation)
  {
    inMemoryContext.AddToList(tokensKey, Enumerable.Repeat(1, ServerCapabilities.IntraServerBufferedChunks));

    await communicationChannel.SubscribeAsync<int>(tokensKey,
      async (message) =>
      {
        await sessionMonitors.SetResourceAndNotify(async () => inMemoryContext.AddToList(tokensKey, 1), correlation);
      }
    );
  }

  private async Task WriteAsync(IAsyncEnumerable<byte[]> dataSource, string topic, string tokensKey, Guid correlation, CancellationToken cancellationToken = default)
  {
    try
    {
      await foreach (var chunk in dataSource.WithCancellation(cancellationToken))
      {
        await WaitForToken(tokensKey, correlation, cancellationToken);
        await communicationChannel.AddToStreamAsync(topic, chunk);
      }
    }
    finally
    {
      await communicationChannel.SignalStreamEndAsync(topic);
    }
  }

  private async IAsyncEnumerable<byte[]> ReadAsync(string topic, string tokensKey, [EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    await foreach (var chunk in communicationChannel.ConsumeStreamAsync(topic, cancellationToken))
    {
      await communicationChannel.PublishAsync(tokensKey, 1);
      yield return chunk;
    }
  }

  private async Task WaitForToken(string tokensKey, Guid correlation, CancellationToken cancellationToken)
  {
    using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    attemptCts.CancelAfter(TimeSpan.FromSeconds(ServerCapabilities.GlobalTimeoutSeconds));

    _ = await sessionMonitors.WaitToGetResource(
      async () => inMemoryContext.PopList<int>(tokensKey),
      (token) => token > 0,
      correlation,
      attemptCts.Token);
  }

  private static string RequestChannelChunksKey(Guid correlation) => $"intra-server-request:chunks:{correlation}";
  private static string ResponseChannelChunksKey(Guid correlation) => $"intra-server-response:chunks:{correlation}";
  private static string RequestChannelTokenKey(Guid correlation) => $"intra-server-request:token:{correlation}";
  private static string ResponseChannelTokenKey(Guid correlation) => $"intra-server-response:token:{correlation}";
  private static string PingKey(Guid correlation) => $"intra-server-ping:{correlation}";
}
