using System.Runtime.CompilerServices;

namespace MiddleMan.Web.Resiliency
{
  public class NoBuffer<T>(IAsyncEnumerable<T> source) : IContentBuffer<T>
  {
    private readonly IAsyncEnumerable<T> sourceStream = source;

    public ValueTask DisposeAsync()
    {
      GC.SuppressFinalize(this);
      return ValueTask.CompletedTask;
    }

    public async IAsyncEnumerable<T> Read([EnumeratorCancellation] CancellationToken cancellationToken)
    {
      await foreach (var chunk in sourceStream.WithCancellation(cancellationToken))
      {
        yield return chunk;
      }
    }
  }
}