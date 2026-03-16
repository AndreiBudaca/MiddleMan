using System.Threading.Channels;
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


  public async Task RegisterRequestSession(Guid correlation)
  {
    inMemoryContext.AddToHash("intraServerRequestSessions", correlation.ToString(), true);
  }

  public async Task ClearRequestSession(Guid correlation)
  {
    inMemoryContext.RemoveFromHash("intraServerRequestSessions", correlation.ToString());
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
    return WriteAsync(dataSource, RequestChannelChunksKey(correlation));
  }

  public Task WriteResponseAsync(IDataWriterAdapter dataWriterAdapter, Guid correlation)
  {
    return WriteResponseAsync(dataWriterAdapter.Adapt(), correlation);
  }

  public Task WriteResponseAsync(IAsyncEnumerable<byte[]> dataSource, Guid correlation)
  {
    return WriteAsync(dataSource, ResponseChannelChunksKey(correlation));
  }

  public async IAsyncEnumerable<byte[]> ReadRequestAsync(Guid correlation)
  {
    await foreach (var chunk in ReadAsync(RequestChannelChunksKey(correlation)))
    {
      yield return chunk;
    }
  }

  public async IAsyncEnumerable<byte[]> ReadResponseAsync(Guid correlation)
  {
    await foreach (var chunk in ReadAsync(ResponseChannelChunksKey(correlation)))
    {
      yield return chunk;
    }
  }

  private async Task WriteAsync(IAsyncEnumerable<byte[]> dataSource, string topic)
  {
    await foreach (var chunk in dataSource)
    {
      await communicationChannel.AddToStreamAsync(topic, chunk);
    }

    await communicationChannel.AddToStreamAsync(topic, []);
  }

  private async IAsyncEnumerable<byte[]> ReadAsync(string topic)
  {
    await foreach (var chunk in communicationChannel.ConsumeStreamAsync(topic, CancellationToken.None))
    {
      yield return chunk;
    }
  }

  private static string RequestChannelChunksKey(Guid correlation) => $"intra-server-request:chunks:{correlation}";
  private static string ResponseChannelChunksKey(Guid correlation) => $"intra-server-response:chunks:{correlation}";
  private static string PingKey(Guid correlation) => $"intra-server-ping:{correlation}";
}
