using MiddleMan.Core;

namespace MiddleMan.Communication.Channels
{
  public interface ICommunicationChannel : IAsyncDisposable
  {
    #region "REQUEST / REPLY (NATS)"

    public Task<TReply?> RequestAsync<TRequest, TReply>(string subject, TRequest request, CancellationToken cancellationToken = default);

    public Task<IAsyncDisposable> SubscribeRequestAsync<TRequest, TReply>(string subject, Func<TRequest, TReply?> handler);

    #endregion

    #region "STREAMS"
    public Task PublishToStreamAsync(string streamKey, IAsyncEnumerable<DataChunk> source, CancellationToken cancellationToken = default);

    public IAsyncEnumerable<DataChunk> ConsumeStreamAsync(string streamKey, CancellationToken cancellationToken);

    public Task DeleteStreamAsync(string streamKey);
    #endregion
  }
}