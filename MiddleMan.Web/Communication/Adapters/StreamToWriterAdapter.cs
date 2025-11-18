
using MiddleMan.Core;

namespace MiddleMan.Web.Communication.Adapters
{
  public class StreamToWriterAdapter(Stream source) : IDataWriterAdapter
  {
    private readonly Stream source = source;

    public async IAsyncEnumerable<byte[]> Adapt()
    {
      var buffer = new byte[ServerCapabilities.MaxContentLength];
      var bytesRead = int.MaxValue;

      while (bytesRead > 0)
      {
        bytesRead = await source.ReadAsync(buffer);
        if (bytesRead == 0)
        {
          yield break;
        }

        if (bytesRead == buffer.Length)
        {
          yield return buffer;
        }
        else
        {
          yield return buffer.Take(bytesRead).ToArray();
        }
      }      
    }
  }
}
