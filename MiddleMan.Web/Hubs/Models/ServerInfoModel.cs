namespace MiddleMan.Web.Hubs.Models
{
  public class ServerInfoModel
  {
    public int MaxMessageLength { get; set; }

    public byte[]? MethodSignature { get; set; }

    public int[] AcceptedVersions { get; set; } = [];
  }
}
