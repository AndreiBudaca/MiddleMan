namespace MiddleMan.Core.Extensions.Classes.AsyncEnumerable
{
  public class EmptyAsyncEnumerable<T> : IAsyncEnumerable<T>
  {
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
      return new EmptyAsyncEnumerator<T>();
    }
  }

  public class EmptyAsyncEnumerator<T> : IAsyncEnumerator<T>
  {
    public T Current => default!;

    public ValueTask DisposeAsync()
    {
      return ValueTask.CompletedTask;
    }

    public ValueTask<bool> MoveNextAsync()
    {
      return new ValueTask<bool>(false);
    }
  }
}