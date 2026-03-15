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
    return communicationChannel.SubscribeAndPeekChannelAsync<string>(PingKey(correlation));
  }

  public Task PingOtherServer(Guid correlation)
  {
    return communicationChannel.PublishAsync(PingKey(correlation), correlation.ToString());
  }

  public Task WriteRequestAsync(IDataWriterAdapter dataWriterAdapter, Guid correlation)
  {
    return WriteRequestAsync(dataWriterAdapter.Adapt(), correlation);
  }

  public async Task WriteRequestAsync(IAsyncEnumerable<byte[]> dataSource, Guid correlation)
  {
    await WriteAsync(dataSource, RequestChannelChunksKey(correlation));
  }

  public Task WriteResponseAsync(IDataWriterAdapter dataWriterAdapter, Guid correlation)
  {
    return WriteResponseAsync(dataWriterAdapter.Adapt(), correlation);
  }

  public async Task WriteResponseAsync(IAsyncEnumerable<byte[]> dataSource, Guid correlation)
  {
    await WriteAsync(dataSource, ResponseChannelChunksKey(correlation));
  }

  public IAsyncEnumerable<byte[]> ReadRequestAsync(Guid correlation)
  {
    return ReadAsync(RequestChannelChunksKey(correlation));
  }

  public IAsyncEnumerable<byte[]> ReadResponseAsync(Guid correlation)
  {
    return ReadAsync(ResponseChannelChunksKey(correlation));
  }

  private async Task WriteAsync(IAsyncEnumerable<byte[]> dataSource, string topic)
  {
    await foreach (var chunk in dataSource)
    {
      await communicationChannel.PublishAsync(topic, chunk);
    }

    await communicationChannel.PublishAsync(topic, []);
  }

  private async IAsyncEnumerable<byte[]> ReadAsync(string topic)
  {
    var chunkChannel = Channel.CreateBounded<byte[]>(
      new BoundedChannelOptions(ServerCapabilities.IntraServerBufferedChunks)
      {
        SingleReader = true,
        SingleWriter = false,
      }
    );

    var semaphore = new SemaphoreSlim(1, 1);

    await communicationChannel.SubscribeAsync(topic, async (chunk) =>
    {
      await semaphore.WaitAsync();
      try
      {
        if (chunk == null || chunk.Length == 0)
        {
          chunkChannel.Writer.Complete();
          return;
        }

        await chunkChannel.Writer.WriteAsync(chunk);
      }
      finally
      {
        semaphore.Release();
      }
    });

    await foreach (var chunk in chunkChannel.Reader.ReadAllAsync())
    {
      yield return chunk;
    }
  }

  private static string RequestChannelChunksKey(Guid correlation) => $"intra-server-request:chunks:{correlation}";
  private static string ResponseChannelChunksKey(Guid correlation) => $"intra-server-response:chunks:{correlation}";
  private static string PingKey(Guid correlation) => $"intra-server-ping:{correlation}";
}
