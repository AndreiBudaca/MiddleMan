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

  public async Task RegisterResponseSession(Guid correlation, bool sameServer)
  {
    if (!sameServer)
    {
      await RegisterTokens(ResponseChannelTokenKey(correlation), correlation);
    }
  }

  public async Task ClearRequestSession(Guid correlation, bool sameServer)
  {
    inMemoryContext.RemoveFromHash("intraServerRequestSessions", correlation.ToString());

    if (!sameServer)
    {
      inMemoryContext.RemoveList(RequestChannelTokenKey(correlation));
    }
  }

  public async Task ClearResponseSession(Guid correlation, bool sameServer)
  {
    if (!sameServer)
    {
      inMemoryContext.RemoveList(ResponseChannelTokenKey(correlation));
    }
  }

  public bool ExistsRequestSession(Guid correlation)
  {
    return inMemoryContext.ExistsInHash("intraServerRequestSessions", correlation.ToString());
  }

  public Task<Task<string?>> WaitForOtherServer(Guid correlation)
  {
    return communicationChannel.PeekChannelAsync<string>(PingKey(correlation));
  }

  public Task PingOtherServer(Guid correlation)
  {
    return communicationChannel.PublishAsync(PingKey(correlation), correlation.ToString());
  }

  public Task WriteRequestAsync(IDataWriterAdapter dataWriterAdapter, Guid correlation)
  {
    return WriteRequestAsync(dataWriterAdapter.Adapt(), correlation);
  }

  public Task WriteRequestAsync(IAsyncEnumerable<byte[]> dataSource, Guid correlation)
  {
    return WriteAsync(dataSource, RequestChannelChunksKey(correlation), RequestChannelTokenKey(correlation), correlation);
  }

  public Task WriteResponseAsync(IDataWriterAdapter dataWriterAdapter, Guid correlation)
  {
    return WriteResponseAsync(dataWriterAdapter.Adapt(), correlation);
  }

  public Task WriteResponseAsync(IAsyncEnumerable<byte[]> dataSource, Guid correlation)
  {
    return WriteAsync(dataSource, ResponseChannelChunksKey(correlation), ResponseChannelTokenKey(correlation), correlation);
  }

  public IAsyncEnumerable<byte[]> ReadRequestAsync(Guid correlation)
  {
    return ReadAsync(RequestChannelChunksKey(correlation), RequestChannelTokenKey(correlation));
  }

  public IAsyncEnumerable<byte[]> ReadResponseAsync(Guid correlation)
  {
    return ReadAsync(ResponseChannelChunksKey(correlation), ResponseChannelTokenKey(correlation));
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

  private async Task WriteAsync(IAsyncEnumerable<byte[]> dataSource, string topic, string tokensKey, Guid correlation)
  {
    await foreach (var chunk in dataSource)
    {
      var _ = await sessionMonitors.WaitToGetResource(
        async () => inMemoryContext.PopList<int>(tokensKey),
        (token) => token > 0,
        correlation);

      await communicationChannel.AddToStreamAsync(topic, chunk);
    }

    await communicationChannel.AddToStreamAsync(topic, []);
  }

  private async IAsyncEnumerable<byte[]> ReadAsync(string topic, string tokensKey)
  {
    await foreach (var chunk in communicationChannel.ConsumeStreamAsync(topic, CancellationToken.None))
    {
      await communicationChannel.PublishAsync(tokensKey, 1);
      yield return chunk;
    }
  }

  private static string RequestChannelChunksKey(Guid correlation) => $"intra-server-request:chunks:{correlation}";
  private static string ResponseChannelChunksKey(Guid correlation) => $"intra-server-response:chunks:{correlation}";
  private static string RequestChannelTokenKey(Guid correlation) => $"intra-server-request:token:{correlation}";
  private static string ResponseChannelTokenKey(Guid correlation) => $"intra-server-response:token:{correlation}";
  private static string PingKey(Guid correlation) => $"intra-server-ping:{correlation}";
}
