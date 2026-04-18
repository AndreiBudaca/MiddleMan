using System.Runtime.CompilerServices;

namespace MiddleMan.Web.Resiliency
{
  public class MemoryBuffer(IAsyncEnumerator<byte[]> contentStream, int maxCapacity) : IContentBuffer<byte[]>
  {
    private readonly List<byte[]> chunks = new(maxCapacity);
    private readonly IAsyncEnumerator<byte[]> enumerator = contentStream;
    private int bufferedChunksCount = 0;
    private bool endOfStreamReached = false;

    public MemoryBuffer(IAsyncEnumerable<byte[]> contentStream, int maxCapacity) : 
      this(contentStream.GetAsyncEnumerator(), maxCapacity) { }

    public async ValueTask DisposeAsync()
    {
      await enumerator.DisposeAsync();
      GC.SuppressFinalize(this);
    }

    public async IAsyncEnumerable<byte[]> Read([EnumeratorCancellation] CancellationToken cancellationToken)
    {
      int currentIndex = 0;
      byte[]? chunk;

      do
      {
        if (cancellationToken.IsCancellationRequested) yield break;
        if (currentIndex < bufferedChunksCount)
        {
          chunk = chunks[currentIndex];
          ++currentIndex;
          yield return chunk;
        }
        else
        {
          if (endOfStreamReached) yield break;
          if (!await enumerator.MoveNextAsync()) 
          {
            endOfStreamReached = true;
            yield break;
          }
          else
          {
            chunk = enumerator.Current;
            if (chunks.Count >= maxCapacity) throw new InvalidOperationException("Buffer capacity exceeded.");

            chunks.Add(chunk);
            ++bufferedChunksCount;
            ++currentIndex;
            yield return chunk;
          }
        }
      } while (true);
    }
  }
}