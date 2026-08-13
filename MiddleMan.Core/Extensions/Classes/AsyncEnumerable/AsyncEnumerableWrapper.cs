namespace MiddleMan.Core.Extensions.Classes.AsyncEnumerable
{
  public class AsyncEnumerableWrapper<T>(T data) : IAsyncEnumerable<T>
  {
    private readonly T _data = data;

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
      return new AsyncEnumerableWrapperEnumerator<T>(_data);
    }
  }
}