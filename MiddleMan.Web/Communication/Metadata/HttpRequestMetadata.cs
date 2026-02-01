namespace MiddleMan.Web.Communication.Metadata
{
  public class HttpRequestMetadata
  {
    public string Method { get; set; } = HttpMethods.Get;

    public string Path { get; set; } = "/";

    public HttpUser? User { get; set; }

    public List<HttpHeader> Headers { get; set; } = [];

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
        Headers.Add(new HttpHeader
        {
          Name = header.Key,
          Value = header.Value,
        });
      }

      User = user;
    }

    public byte[] SerializeJson()
    {
      var jsonBytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(this);
      var metadataLength = BitConverter.GetBytes(jsonBytes.Length);

      return metadataLength.Concat(jsonBytes).ToArray();
    }
  }
}
