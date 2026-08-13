using MiddleMan.Core;
using MiddleMan.Web.Communication.Metadata;

namespace MiddleMan.Web.Communication
{
  public class ResponseData
  {
    public HttpResponseMetadata? Metadata { get; set; }
    public IAsyncEnumerable<DataChunk>? Data { get; set; }
  }
}