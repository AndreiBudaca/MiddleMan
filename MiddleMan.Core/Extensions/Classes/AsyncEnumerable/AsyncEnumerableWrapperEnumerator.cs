namespace MiddleMan.Core.Extensions.Classes.AsyncEnumerable
{
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