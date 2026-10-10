using MiddleMan.Core.Extensions.Classes.AsyncEnumerable;

namespace MiddleMan.Core.Extensions
{
  public static class AsyncEnumerableExtensions
  {
    public static async Task<AsyncEnumResult> EnumerateUntil(this IAsyncEnumerable<DataChunk> data, int bytesToReceive, CancellationToken cancellationToken)
    {
      var received = new DataChunk(new byte[bytesToReceive], 0);
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
          if (item == null || item.Length == 0) continue;

          // Calculate how many bytes we can copy from this item
          var bytesToCopy = Math.Min(item.Length, bytesToReceive - bytesCopied);

          received.Append(item.Data, bytesToCopy);

          // Update counters
          bytesCopied += bytesToCopy;
          totalBytesReceived += item.Length;

          // Check if we've received enough bytes
          if (bytesCopied >= bytesToReceive)
          {
            // No remaining data in this item, just return the enumerator as is
            if (bytesToCopy == item.Length)
            {
              enumeratorTransferred = true;

              return new AsyncEnumResult
              {
                Received = received,
                Next = DrainEnumerator(enumerator)
              };
            }

            // Remaining data in this item, create a new DataChunk for the remaining data and return it with the enumerator
            var remaining = new DataChunk(item.Length - bytesToCopy);
            Array.Copy(item.Data, bytesToCopy, remaining.Data, 0, remaining.Length);

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

      throw new InvalidDataException($"Invalid content length. Expected {bytesToReceive} bytes, but only got {totalBytesReceived}.");
    }

    public static async IAsyncEnumerable<T> PrependItems<T>(this IAsyncEnumerator<T> enumerator, T? item)
    {
      try
      {
        if (item != null) yield return item;

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

    private static async IAsyncEnumerable<T> DrainEnumerator<T>(IAsyncEnumerator<T> enumerator)
    {
      try
      {
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

    public static IAsyncEnumerable<T> PrependItems<T>(this IAsyncEnumerable<T> enumerable, T item, CancellationToken cancellationToken)
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
