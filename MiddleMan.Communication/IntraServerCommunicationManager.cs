using MiddleMan.Communication.Channels;
using MiddleMan.Core;

namespace MiddleMan.Communication;

public class IntraServerCommunicationManager(ICommunicationChannel communicationChannel)
{
  private readonly ICommunicationChannel communicationChannel = communicationChannel;

  public async Task ClearRequestSession(Guid correlation, bool error = false)
  {
    // On error we make sure the stream is also deleted
    // On success the stream needs to remain so the other server can complete reading from it
    if (error) await MarkRequestHandled(correlation);
  }

  public Task MarkRequestHandled(Guid correlation)
  {
    return communicationChannel.DeleteStreamAsync(RequestChannelChunksKey(correlation));
  }

  public async Task ClearResponseSession(Guid correlation, bool error = false)
  {
    // On error we make sure the stream is also deleted
    // On success the stream needs to remain so the other server can complete reading from it
    if (error) await MarkResponseHandled(correlation);
  }

  public Task MarkResponseHandled(Guid correlation)
  {
    return communicationChannel.DeleteStreamAsync(ResponseChannelChunksKey(correlation));
  }

  public Task WriteRequestAsync(IAsyncEnumerable<DataChunk> dataSource, Guid correlation, CancellationToken cancellationToken = default)
  {
    return communicationChannel.PublishToStreamAsync(RequestChannelChunksKey(correlation), dataSource, cancellationToken);
  }

  public Task WriteResponseAsync(IAsyncEnumerable<DataChunk> dataSource, Guid correlation, CancellationToken cancellationToken = default)
  {
    return communicationChannel.PublishToStreamAsync(ResponseChannelChunksKey(correlation), dataSource, cancellationToken);
  }

  public IAsyncEnumerable<DataChunk> ReadRequestAsync(Guid correlation, CancellationToken cancellationToken = default)
  {
    return communicationChannel.ConsumeStreamAsync(RequestChannelChunksKey(correlation), cancellationToken);
  }

  public IAsyncEnumerable<DataChunk> ReadResponseAsync(Guid correlation, CancellationToken cancellationToken = default)
  {
    return communicationChannel.ConsumeStreamAsync(ResponseChannelChunksKey(correlation), cancellationToken);
  }

  private static string RequestChannelChunksKey(Guid correlation) => $"intra-server.request.chunks.{correlation}";
  private static string ResponseChannelChunksKey(Guid correlation) => $"intra-server.response.chunks.{correlation}";
}
