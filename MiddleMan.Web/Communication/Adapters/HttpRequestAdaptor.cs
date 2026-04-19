using MiddleMan.Core;
using MiddleMan.Communication.Adapters; 

namespace MiddleMan.Web.Communication.Adapters
{
  public class HttpRequestAdaptor(HttpRequest request) : IDataWriterAdaptor
  {
    private readonly Stream source = request.Body;

    public async IAsyncEnumerable<byte[]> Adapt()
    {
      var buffer = new byte[ServerCapabilities.MaxChunkSize];
      var bytesRead = await source.ReadAsync(buffer.AsMemory(0, ServerCapabilities.MaxChunkSize));

      while (bytesRead > 0)
      {
        if (bytesRead == buffer.Length)
        {
          yield return buffer;
        }
        else
        {
          yield return buffer.Take(bytesRead).ToArray();
        }

        bytesRead = await source.ReadAsync(buffer.AsMemory(0, ServerCapabilities.MaxChunkSize));
      }
    }
  }
}
