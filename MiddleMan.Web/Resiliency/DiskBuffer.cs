using System.Runtime.CompilerServices;
using MiddleMan.Core;

namespace MiddleMan.Web.Resiliency
{
  public class DiskBuffer(IAsyncEnumerator<byte[]> enumerator) : IContentBuffer<byte[]>
  {
    private readonly List<int> chunkSizes = [];
    private readonly FileStream fileStream = new(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.Read, ServerCapabilities.MaxChunkSize, FileOptions.Asynchronous | FileOptions.DeleteOnClose);
    private readonly IAsyncEnumerator<byte[]> enumerator = enumerator;
    private bool endOfStreamReached = false;

    public DiskBuffer(IAsyncEnumerable<byte[]> contentStream) : this(contentStream.GetAsyncEnumerator()) { }

    public async IAsyncEnumerable<byte[]> Read([EnumeratorCancellation] CancellationToken cancellationToken)
    {
      var currentIndex = 0;
      fileStream.Seek(0, SeekOrigin.Begin);

      while (true)
      {
        if (cancellationToken.IsCancellationRequested) yield break;

        if (currentIndex < chunkSizes.Count)
        {
          var chunkSize = chunkSizes[currentIndex];
          var buffer = new byte[chunkSize];
          var bytesRead = await fileStream.ReadAsync(buffer.AsMemory(0, chunkSize), cancellationToken);
          if (bytesRead != chunkSize) throw new IOException("Failed to read the expected number of bytes from the buffer.");
          ++currentIndex;
          yield return buffer;
        }
        else
        {
          if (endOfStreamReached) yield break;
          if (!await enumerator.MoveNextAsync()) 
          {
            endOfStreamReached = true;
            yield break;
          }

          var chunk = enumerator.Current;
          await fileStream.WriteAsync(chunk, cancellationToken);
          chunkSizes.Add(chunk.Length);
          ++currentIndex;
          yield return chunk;
        }
      }
    }

    public async ValueTask DisposeAsync()
    {
      await fileStream.DisposeAsync();
      await enumerator.DisposeAsync();

      GC.SuppressFinalize(this);
    }
  }
}