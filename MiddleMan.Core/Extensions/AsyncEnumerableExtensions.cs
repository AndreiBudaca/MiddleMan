using System.Runtime.CompilerServices;

namespace MiddleMan.Core.Extensions
{
  public class AsyncEnumResult<T>
  {
    public required T[] Received { get; set; }

    public required T[] CurrentEnumerationItem { get; set; }
  }

  public static class AsyncEnumerableExtensions
  {
    public static async Task<AsyncEnumResult<T>> EnumerateUntil<T>(this IAsyncEnumerable<T[]> data, int bytesToReceive, int offset, CancellationToken cancellationToken)
    {
      var received = new T[bytesToReceive];
      var totalBytesReceived = 0;
      var bytesCoppied = 0;

      await foreach (var item in data.WithCancellation(cancellationToken))
      {
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

        Array.Copy(item, copyFromIndex, received, bytesCoppied, bytesToCopy);

        // Update counters
        bytesCoppied += bytesToCopy;
        totalBytesReceived += item.Length;

        // Check if we've received enough bytes
        if (bytesCoppied >= bytesToReceive)
        {
          return new AsyncEnumResult<T>
          {
            Received = received,
            CurrentEnumerationItem = item.Skip(copyFromIndex + bytesToCopy).ToArray()
          };
        }
      }

      throw new InvalidDataException($"Invalid content lenght. Expected to read {bytesToReceive} from {offset}, but only got {totalBytesReceived}.");
    }

    public static async IAsyncEnumerable<T> PrependItems<T>(this IAsyncEnumerable<T> source, [EnumeratorCancellation] CancellationToken cancellationToken, params T[] items)
    {
      foreach (var item in items)
      {
        yield return item;
      }

      await foreach (var item in source.WithCancellation(cancellationToken))
      {
        yield return item;
      }
    }
  }
}
