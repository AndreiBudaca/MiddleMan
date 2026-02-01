using MiddleMan.Core;
using MiddleMan.Web.Communication.Metadata;

namespace MiddleMan.Web.Communication.Adapters
{
  public class HttpRequestAdapterAdapter(HttpRequest request) : IDataWriterAdapter
  {
    private readonly HttpRequestMetadata metadata = new (request);
    private readonly Stream source = request.Body;

    public async IAsyncEnumerable<byte[]> Adapt()
    {
      yield return metadata.SerializeJson();

      var buffer = new byte[ServerCapabilities.MaxContentLength];
      var bytesRead = await source.ReadAsync(buffer.AsMemory(0, ServerCapabilities.MaxContentLength));

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

        bytesRead = await source.ReadAsync(buffer.AsMemory(0, ServerCapabilities.MaxContentLength));
      }
    }
  }
}
