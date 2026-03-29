namespace MiddleMan.Communication.Channels
{
  public interface ICommunicationChannel
  {
    #region "PUB / SUB"
    public Task SubscribeAsync<T>(string topic, Func<T, Task> onMessageReceived);

    public Task SubscribeAsync(string topic, Func<byte[], Task> onMessageReceived);

    public Task<Task<T?>> PeekChannelAsync<T>(string topic, CancellationToken cancellationToken = default);

    public Task UnsubscribeAsync(string topic);

    public Task PublishAsync<T>(string topic, T message);
    
    public Task PublishAsync(string topic, byte[] message);
    #endregion

    #region "STREAMS"
    public Task AddToStreamAsync(string streamKey, byte[] data);

    public IAsyncEnumerable<byte[]> ConsumeStreamAsync(string streamKey, string heartbeatKey, CancellationToken cancellationToken);

    public Task RefreshHeartbeatAsync(string heartbeatKey, TimeSpan ttl);

    public Task<bool> HeartbeatExistsAsync(string heartbeatKey);

    public Task DeleteKeyAsync(string key);
    #endregion
  }
}