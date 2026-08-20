using MiddleMan.Core;
using MiddleMan.Core.Extensions;

namespace MiddleMan.Web.Communication.Metadata
{
  public class HttpRequestMetadata
  {
    public string Method { get; set; } = HttpMethods.Get;

    public string Path { get; set; } = "/";

    public HttpUser? User { get; set; }

    public Dictionary<string, string?> Headers { get; set; } = [];

    public HttpRequestMetadata()
    {

    }

    public HttpRequestMetadata(HttpRequest request, HttpUser? user = null)
    {
      List<string> headersToOmit = ["Cookie"];

      Method = request.Method;

      var pathParts = request.Path.Value?.Split('/') ?? [];

      Path = "/" + string.Join('/', pathParts.Skip(5)) + request.QueryString;
      foreach (var header in request.Headers.Where(x => !headersToOmit.Contains(x.Key)))
      {
        Headers.Add(header.Key, header.Value);
      }

      User = user;
    }
  }
}
