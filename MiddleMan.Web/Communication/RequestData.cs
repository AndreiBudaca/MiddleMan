using MiddleMan.Core;
using MiddleMan.Web.Communication.Metadata;

namespace MiddleMan.Web.Communication
{
  public class RequestData
  {
    public HttpRequestMetadata? Metadata { get; set; }
    public required IAsyncEnumerable<DataChunk> Data { get; set; }
  }
}