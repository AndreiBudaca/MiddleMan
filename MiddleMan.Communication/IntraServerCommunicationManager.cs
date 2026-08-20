using System.Runtime.CompilerServices;
using MiddleMan.Communication.Channels;
using MiddleMan.Communication.SyncMechanisms;
using MiddleMan.Core;

namespace MiddleMan.Communication;

public class IntraServerCommunicationManager(ICommunicationChannel communicationChannel)
{
  private readonly static AsyncResourceMonitor<Guid> sessionMonitors = new();
  private readonly ICommunicationChannel communicationChannel = communicationChannel;
  private TokenCounter? requestTokenCounter;
  private TokenCounter? responseTokenCounter;

  public async Task RegisterRequestSession(Guid correlation)
  {
    requestTokenCounter = new TokenCounter();

    await communicationChannel.CreateStreamAsync(RequestChannelChunksKey(correlation));
    await communicationChannel.CreateStreamAsync(ResponseChannelChunksKey(correlation));
    await RegisterTokens(requestTokenCounter, RequestChannelTokenKey(correlation), correlation);
  }

  public async Task RegisterResponseSession(Guid correlation)
  {
    responseTokenCounter = new TokenCounter();
    await RegisterTokens(responseTokenCounter, ResponseChannelTokenKey(correlation), correlation);
  }

  public async Task ClearRequestSession(Guid correlation)
  {
    await communicationChannel.UnsubscribeAsync(RequestChannelTokenKey(correlation));
    await communicationChannel.DeleteStreamAsync(RequestChannelChunksKey(correlation));
    await communicationChannel.DeleteStreamAsync(ResponseChannelChunksKey(correlation));
  }

  public async Task ClearResponseSession(Guid correlation)
  {
    await communicationChannel.UnsubscribeAsync(ResponseChannelTokenKey(correlation));
  }

  public Task WriteRequestAsync(IAsyncEnumerable<DataChunk> dataSource, Guid correlation, CancellationToken cancellationToken = default)
  {
    if (requestTokenCounter == null) throw new InvalidOperationException("Request session not registered. Call RegisterRequestSession first.");

    return WriteAsync(dataSource, RequestChannelChunksKey(correlation), requestTokenCounter, cancellationToken);
  }

  public Task WriteResponseAsync(IAsyncEnumerable<DataChunk> dataSource, Guid correlation, CancellationToken cancellationToken = default)
  {
    if (responseTokenCounter == null) throw new InvalidOperationException("Response session not registered. Call RegisterResponseSession first.");

    return WriteAsync(dataSource, ResponseChannelChunksKey(correlation), responseTokenCounter, cancellationToken);
  }

  public IAsyncEnumerable<DataChunk> ReadRequestAsync(Guid correlation, CancellationToken cancellationToken = default)
  {
    return ReadAsync(RequestChannelChunksKey(correlation), RequestChannelTokenKey(correlation), cancellationToken);
  }

  public IAsyncEnumerable<DataChunk> ReadResponseAsync(Guid correlation, CancellationToken cancellationToken = default)
  {
    return ReadAsync(ResponseChannelChunksKey(correlation), ResponseChannelTokenKey(correlation), cancellationToken);
  }

  private async Task RegisterTokens(TokenCounter counter, string tokensKey, Guid correlation)
  {
    await communicationChannel.SubscribeAsync<int>(tokensKey,
      async (message) =>
      {
        await sessionMonitors.SetResourceAndNotify(async () => counter.Increment(), correlation);
      }
    );
  }

  private async Task WriteAsync(IAsyncEnumerable<DataChunk> dataSource, string topic, TokenCounter counter, CancellationToken cancellationToken = default)
  {
    try
    {
      await foreach (var chunk in dataSource.WithCancellation(cancellationToken))
      {
        await WaitForToken(counter, cancellationToken);
        await communicationChannel.AddToStreamAsync(topic, chunk);
      }

      await communicationChannel.SignalStreamEndAsync(topic);
    }
    catch (Exception ex)
    {
      await communicationChannel.TerminateWithErrorAsync(topic, ex.Message);
      throw;
    }
  }

  private async IAsyncEnumerable<DataChunk> ReadAsync(string topic, string tokensKey, [EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    await foreach (var chunk in communicationChannel.ConsumeStreamAsync(topic, cancellationToken))
    {
      await communicationChannel.PublishAsync(tokensKey, 1);
      yield return chunk;
    }
  }

  private static async Task WaitForToken(TokenCounter counter, CancellationToken cancellationToken)
  {
    using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    attemptCts.CancelAfter(TimeSpan.FromSeconds(ServerCapabilities.GlobalTimeoutSeconds));

    await counter.Decrement(attemptCts.Token);
  }

  private static string RequestChannelChunksKey(Guid correlation) => $"intra-server-request:chunks:{correlation}";
  private static string ResponseChannelChunksKey(Guid correlation) => $"intra-server-response:chunks:{correlation}";
  private static string RequestChannelTokenKey(Guid correlation) => $"intra-server-request:token:{correlation}";
  private static string ResponseChannelTokenKey(Guid correlation) => $"intra-server-response:token:{correlation}";
}
