
using MiddleMan.Core;

namespace MiddleMan.Web.Communication.Adapters
{
  public class StreamToWriterAdapter(Stream source) : IDataWriterAdapter
  {
    private readonly Stream source = source;

    public async IAsyncEnumerable<byte[]> Adapt()
    {
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
