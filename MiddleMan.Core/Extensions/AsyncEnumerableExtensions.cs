using System.Runtime.CompilerServices;

namespace MiddleMan.Core.Extensions
{
  public static class AsyncEnumerableExtensions
  {
    public static async Task<AsyncEnumResult<T>> EnumerateUntil<T>(this IAsyncEnumerable<T[]> data, int bytesToReceive, int offset, CancellationToken cancellationToken)
    {
      var received = new T[bytesToReceive];
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

          Array.Copy(item, copyFromIndex, received, bytesCopied, bytesToCopy);

          // Update counters
          bytesCopied += bytesToCopy;
          totalBytesReceived += item.Length;

          // Check if we've received enough bytes
          if (bytesCopied >= bytesToReceive)
          {
            var remaining = item.Skip(copyFromIndex + bytesToCopy).ToArray();
            enumeratorTransferred = true;
            return new AsyncEnumResult<T>
            {
              Received = received,
              Next = enumerator.PrependItems(remaining, CancellationToken.None)
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

    public static async IAsyncEnumerable<T[]> PrependItems<T>(this IAsyncEnumerator<T[]> enumerator, T[] item, [EnumeratorCancellation] CancellationToken cancellationToken)
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

    public static IAsyncEnumerable<T[]> PrependItems<T>(this IAsyncEnumerable<T[]> enumerable, T[] item, CancellationToken cancellationToken)
    {
      return enumerable.GetAsyncEnumerator(cancellationToken).PrependItems(item, cancellationToken);
    }

    public static async Task<byte[]> ReadAllBytes(this IAsyncEnumerable<byte[]> data, CancellationToken cancellationToken)
    {
      using var memoryStream = new MemoryStream();

      await foreach (var chunk in data.WithCancellation(cancellationToken))
      {
        if (chunk != null)
        {
          await memoryStream.WriteAsync(chunk, cancellationToken);
        }
      }

      return memoryStream.ToArray();
    }
  
    public static IAsyncEnumerable<T> AsAsyncEnumerable<T>(this T data)
    {
      return new AsyncEnumerableWrapper<T>(data);
    }
  }

  public class AsyncEnumResult<T>
  {
    public required T[] Received { get; set; }

    public required IAsyncEnumerable<T[]> Next { get; set; }
  }

  public class AsyncEnumerableWrapper<T>(T data) : IAsyncEnumerable<T>
  {
    private readonly T _data = data;

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
      return new AsyncEnumerableWrapperEnumerator<T>(_data);
    }
  }

  public class AsyncEnumerableWrapperEnumerator<T>(T data) : IAsyncEnumerator<T>
  {
    private readonly T _data = data;
    private bool _hasMoved = false;

    public T Current => _data;

    public ValueTask<bool> MoveNextAsync()
    {
      if (!_hasMoved)
      {
        _hasMoved = true;
        return ValueTask.FromResult(true);
      }

      return ValueTask.FromResult(false);
    }

    public ValueTask DisposeAsync()
    {
      return ValueTask.CompletedTask;
    }
  }
}
