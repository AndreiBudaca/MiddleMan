using MiddleMan.Core;

namespace MiddleMan.Communication.Channels
{
  public interface ICommunicationChannel
  {
    #region "PUB / SUB"
    public Task SubscribeAsync<T>(string topic, Func<T, Task> onMessageReceived);

    public Task<Task<T?>> PeekChannelAsync<T>(string topic, CancellationToken cancellationToken = default);

    public Task UnsubscribeAsync(string topic);

    public Task PublishAsync<T>(string topic, T message);
    
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