namespace MiddleMan.Core.Extensions.Classes.AsyncEnumerable
{
  public class AsyncEnumResult<T>
  {
    public required T[] Received { get; set; }

    public required IAsyncEnumerable<T[]> Next { get; set; }
  }
}