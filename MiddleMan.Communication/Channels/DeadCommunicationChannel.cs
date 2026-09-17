using System.Runtime.CompilerServices;
using MiddleMan.Core;

namespace MiddleMan.Communication.Channels
{
  public class DeadCommunicationChannel : ICommunicationChannel
  {
    public Task PublishToStreamAsync(string streamKey, IAsyncEnumerable<DataChunk> source, CancellationToken cancellationToken = default)
    {
      return Task.CompletedTask;
    }

    public async IAsyncEnumerable<DataChunk> ConsumeStreamAsync(string streamKey, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
      yield break;
    }

    public Task DeleteStreamAsync(string streamKey)
    {
      return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
      GC.SuppressFinalize(this);
      return ValueTask.CompletedTask;
    }

    public Task<TReply?> RequestAsync<TRequest, TReply>(string subject, TRequest request, CancellationToken cancellationToken = default)
    {
      return Task.FromResult<TReply?>(default);
    }

    public Task<IAsyncDisposable> SubscribeRequestAsync<TRequest, TReply>(string subject, Func<TRequest, TReply?> handler)
    {
      return Task.FromResult<IAsyncDisposable>(NullSubscription.Instance);
    }

    private sealed class NullSubscription : IAsyncDisposable
    {
      public static readonly NullSubscription Instance = new();

      public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
  }
}
