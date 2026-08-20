namespace MiddleMan.Core.Extensions.Classes.AsyncEnumerable
{
  public class AsyncEnumResult
  {
    public required DataChunk Received { get; set; }

    public required IAsyncEnumerable<DataChunk> Next { get; set; }
  }
}