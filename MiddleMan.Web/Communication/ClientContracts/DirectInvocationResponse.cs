using MiddleMan.Web.Communication.Metadata;

namespace MiddleMan.Web.Communication.ClientContracts
{
  public class DirectInvocationResponse
  {
    public HttpResponseMetadata? Metadata { get; set; }
    public byte[] Data { get; set; } = [];
  }
}