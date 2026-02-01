namespace MiddleMan.Web.Communication.Metadata
{
  public class HttpRequestMetadata
  {
    public string Method { get; set; } = HttpMethods.Get;

    public string Path { get; set; } = "/";

    public List<HttpHeader> Headers { get; set; } = [];

    public HttpRequestMetadata()
    {
      
    }

    public HttpRequestMetadata(HttpRequest request)
    {
      Method = request.Method;
      Path = request.Path + request.QueryString;
      foreach (var header in request.Headers)
      {
        Headers.Add(new HttpHeader
        {
          Name = header.Key,
          Value = header.Value,
        });
      }
    }

    public byte[] SerializeJson()
    {
      var jsonBytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(this);
      var metadataLength = BitConverter.GetBytes(jsonBytes.Length);

      return metadataLength.Concat(jsonBytes).ToArray();
    }
  }
}
