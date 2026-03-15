using System.Threading.Channels;

namespace MiddleMan.Communication.Channels
{
  public interface ICommunicationChannel
  {
    #region "PUB / SUB"
    public Task SubscribeAsync<T>(string topic, Func<T, Task> onMessageReceived);

    public Task SubscribeAsync(string topic, Func<byte[], Task> onMessageReceived);

    public Task<Task<T?>> SubscribeAndPeekChannelAsync<T>(string topic);

    public Task UnsubscribeAsync(string topic);

    public Task PublishAsync<T>(string topic, T message);
    
    public Task PublishAsync(string topic, byte[] message);
    #endregion
  }
}