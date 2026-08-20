using System.Runtime.CompilerServices;
using MiddleMan.Core;

namespace MiddleMan.Communication.Channels
{
  public class DeadCommunicationChannel : ICommunicationChannel
  {
    public Task AddToStreamAsync(string streamKey, DataChunk data)
    {
      return Task.CompletedTask;
    }

    public async IAsyncEnumerable<DataChunk> ConsumeStreamAsync(string streamKey, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
      yield break;
    }

    public Task CreateStreamAsync(string streamKey)
    {
      return Task.CompletedTask;
    }

    public Task DeleteStreamAsync(string streamKey)
    {
      return Task.CompletedTask;
    }

    public Task<Task<T?>> PeekChannelAsync<T>(string topic, CancellationToken cancellationToken = default)
    {
      return Task.FromResult(Task.FromResult<T?>(default));
    }

    public Task PublishAsync<T>(string topic, T message)
    {
      return Task.CompletedTask;
    }

    public Task SignalStreamEndAsync(string streamKey)
    {
      return Task.CompletedTask;
    }

    public Task SubscribeAsync<T>(string topic, Func<T, Task> onMessageReceived)
    {
      return Task.CompletedTask;
    }

    public Task TerminateWithErrorAsync(string streamKey, string errorMessage)
    {
      return Task.CompletedTask;
    }

    public Task UnsubscribeAsync(string topic)
    {
      return Task.CompletedTask;
    }
  }
}