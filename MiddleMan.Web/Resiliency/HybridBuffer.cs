using System.Runtime.CompilerServices;
using MiddleMan.Core;

namespace MiddleMan.Web.Resiliency
{
  public class HybridBuffer : IContentBuffer<DataChunk>
  {
    private readonly int maxMemoryCapacity;
    private readonly MemoryBuffer<DataChunk> memoryBuffer;
    private readonly DiskBuffer diskBuffer;

    public HybridBuffer(IAsyncEnumerable<DataChunk> contentStream, int maxMemoryCapacity)
    {
      var enumerator = contentStream.GetAsyncEnumerator();

      this.maxMemoryCapacity = maxMemoryCapacity;
      memoryBuffer = new MemoryBuffer<DataChunk>(enumerator, maxMemoryCapacity);
      diskBuffer = new DiskBuffer(enumerator);
    }

    public async ValueTask DisposeAsync()
    {
      await memoryBuffer.DisposeAsync();
      await diskBuffer.DisposeAsync();

      GC.SuppressFinalize(this);
    }

    public async IAsyncEnumerable<DataChunk> Read([EnumeratorCancellation] CancellationToken cancellationToken)
    {
      var currentIndex = 0;
      await using var memoryEnumerator = memoryBuffer.Read(cancellationToken).GetAsyncEnumerator(cancellationToken);
      await using var diskEnumerator = diskBuffer.Read(cancellationToken).GetAsyncEnumerator(cancellationToken);

      do
      {
        if (cancellationToken.IsCancellationRequested) yield break;

        if (currentIndex < maxMemoryCapacity && await memoryEnumerator.MoveNextAsync())
        {
          ++currentIndex;
          yield return memoryEnumerator.Current;
        }
        else if (await diskEnumerator.MoveNextAsync())
        {
          ++currentIndex;
          yield return diskEnumerator.Current;
        }
        else
        {
          yield break;
        }
      } while (true);
    }
  }
}