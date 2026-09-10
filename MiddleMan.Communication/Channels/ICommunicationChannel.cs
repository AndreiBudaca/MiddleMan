using MiddleMan.Core;

namespace MiddleMan.Communication.Channels
{
  public interface ICommunicationChannel : IAsyncDisposable
  {
    #region "PUB / SUB"
    public Task SubscribeAsync<T>(string topic, Func<T, Task> onMessageReceived, CancellationToken cancellationToken = default);

    public Task UnsubscribeAsync(string topic);

    public Task PublishAsync<T>(string subject, T message, CancellationToken cancellationToken = default);

    #endregion

    #region "REQUEST / REPLY (NATS)"

    public Task<TReply?> RequestAsync<TRequest, TReply>(string subject, TRequest request, CancellationToken cancellationToken = default);

    public Task SubscribeRequestAsync<TRequest, TReply>(string subject, Func<TRequest, Task<TReply?>> handler, CancellationToken cancellationToken = default);

    #endregion

    #region "STREAMS"
    public Task CreateStreamAsync(string streamKey);

    public Task AddToStreamAsync(string streamKey, DataChunk data);
    
    public Task TerminateWithErrorAsync(string streamKey, string errorMessage);

    public Task SignalStreamEndAsync(string streamKey);

    public IAsyncEnumerable<DataChunk> ConsumeStreamAsync(string streamKey, CancellationToken cancellationToken);

    public Task DeleteStreamAsync(string streamKey);
    #endregion
  }
}