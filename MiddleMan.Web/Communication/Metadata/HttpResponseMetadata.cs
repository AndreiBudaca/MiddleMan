namespace MiddleMan.Web.Communication.Metadata
{
  public class HttpResponseMetadata
  {
    public int ResponseCode { get; set; } = 200;

    public List<HttpHeader> Headers { get; set; } = [];

    public void Apply(HttpResponse response)
    {
      response.StatusCode = ResponseCode;
      foreach (var header in Headers)
      {
        response.Headers[header.Name] = header.Value;
      }
    }
  }
}
