using System.Runtime.CompilerServices;

namespace MiddleMan.Web.Resiliency
{
  public class NoBuffer(IAsyncEnumerable<byte[]> source) : IContentBuffer
  {
    private readonly IAsyncEnumerable<byte[]> sourceStream = source;

    public ValueTask DisposeAsync()
    {
      GC.SuppressFinalize(this);
      return ValueTask.CompletedTask;
    }

    public async IAsyncEnumerable<byte[]> Read([EnumeratorCancellation] CancellationToken cancellationToken)
    {
      await foreach (var chunk in sourceStream.WithCancellation(cancellationToken))
      {
        yield return chunk;
      }
    }
  }
}