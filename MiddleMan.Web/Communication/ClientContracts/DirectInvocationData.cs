using MiddleMan.Web.Communication.Metadata;

namespace MiddleMan.Web.Communication.ClientContracts
{
  public class DirectInvocationData
  {
    public HttpRequestMetadata? Metadata { get; set; }
    public byte[] Data { get; set; } = [];
  }
}