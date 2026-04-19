namespace MiddleMan.Core.Extensions
{
  public static class ByteArrayExtensions
  {
    public static async IAsyncEnumerable<byte[]> AsAsyncEnumerable(this byte[] source)
    {
      yield return source;
    }
  }
}