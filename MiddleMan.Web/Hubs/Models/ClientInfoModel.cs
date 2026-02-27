namespace MiddleMan.Web.Hubs.Models
{
  public class ClientInfoModel
  {
    public int Version { get; set; } = -1;

    public bool SupportsStreaming { get; set; }

    public bool SendHTTPMetadata { get; set; }
  }
}