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

    public Task PublishAsync<T>(string subject, T message, CancellationToken cancellationToken)
    {
      return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
      return ValueTask.CompletedTask;
    }

    public Task<TReply?> RequestAsync<TRequest, TReply>(string subject, TRequest request, CancellationToken cancellationToken = default)
    {
      return Task.FromResult<TReply?>(default);
    }

    public Task SignalStreamEndAsync(string streamKey)
    {
      return Task.CompletedTask;
    }

    public Task SubscribeAsync<T>(string topic, Func<T, Task> onMessageReceived, CancellationToken cancellationToken = default)
    {
      return Task.CompletedTask;
    }

    public Task SubscribeRequestAsync<TRequest, TReply>(string subject, Func<TRequest, Task<TReply?>> handler, CancellationToken cancellationToken = default)
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