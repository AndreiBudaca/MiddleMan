using System.Runtime.CompilerServices;
using MiddleMan.Core.Extensions.Classes.AsyncEnumerable;

namespace MiddleMan.Core.Extensions
{
  public static class AsyncEnumerableExtensions
  {
    public static async Task<AsyncEnumResult> EnumerateUntil(this IAsyncEnumerable<DataChunk> data, int bytesToReceive, int offset, CancellationToken cancellationToken)
    {
      var received = new DataChunk(bytesToReceive);
      var totalBytesReceived = 0;
      var bytesCopied = 0;

      var enumerator = data.GetAsyncEnumerator(cancellationToken);
      var enumeratorTransferred = false;

      try
      {
        while (await enumerator.MoveNextAsync())
        {
          var item = enumerator.Current;

          // Nothing to read from this item, continue to read
          if (item == null) continue;

          // Still haven't reached the offset, continue to read
          if (totalBytesReceived + item.Length <= offset)
          {
            totalBytesReceived += item.Length;
            continue;
          }

          // Calculate how many bytes we can copy from this item
          var copyFromIndex = totalBytesReceived > offset ? 0 : offset - totalBytesReceived;
          var bytesToCopy = Math.Min(item.Length - copyFromIndex, bytesToReceive - totalBytesReceived + offset);

          received.Append(item.Data, bytesToCopy);

          // Update counters
          bytesCopied += bytesToCopy;
          totalBytesReceived += item.Length;

          // Check if we've received enough bytes
          if (bytesCopied >= bytesToReceive)
          {
            var remainingIndex = copyFromIndex + bytesToCopy;
            var remaining = new DataChunk(item.Length - remainingIndex);
            Array.Copy(item.Data, remainingIndex, remaining.Data, 0, remaining.Length);

            enumeratorTransferred = true;
            return new AsyncEnumResult
            {
              Received = received,
              Next = enumerator.PrependItems(remaining)
            };
          }
        }
      }
      finally
      {
        if (!enumeratorTransferred)
        {
          await enumerator.DisposeAsync();
        }
      }

      throw new InvalidDataException($"Invalid content lenght. Expected to read {bytesToReceive} from {offset}, but only got {totalBytesReceived}.");
    }

    public static async IAsyncEnumerable<DataChunk> PrependItems(this IAsyncEnumerator<DataChunk> enumerator, DataChunk item)
    {
      try
      {
        if (item.Length > 0)
          yield return item;

        while (await enumerator.MoveNextAsync())
        {
          yield return enumerator.Current;
        }
      }
      finally
      {
        await enumerator.DisposeAsync();
      }
    }

    public static IAsyncEnumerable<DataChunk> PrependItems(this IAsyncEnumerable<DataChunk> enumerable, DataChunk item, CancellationToken cancellationToken)
    {
      return enumerable.GetAsyncEnumerator(cancellationToken).PrependItems(item);
    }

    public static async Task<byte[]> ReadAllBytes(this IAsyncEnumerable<DataChunk> data, int? maxLength = null, CancellationToken cancellationToken = default)
    {
      using var memoryStream = new MemoryStream();

      var totalBytesRead = 0;
      await foreach (var chunk in data.WithCancellation(cancellationToken))
      {
        totalBytesRead += chunk.Length;
        if (maxLength.HasValue && totalBytesRead >= maxLength.Value) break;

        await memoryStream.WriteAsync(chunk.Data.AsMemory(0, chunk.Length), cancellationToken);
      }

      return memoryStream.ToArray();
    }

    public static IAsyncEnumerable<T> AsAsyncEnumerable<T>(this T data)
    {
      return new AsyncEnumerableWrapper<T>(data);
    }
  }
}
