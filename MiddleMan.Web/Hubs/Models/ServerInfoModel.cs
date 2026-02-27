namespace MiddleMan.Web.Hubs.Models
{
  public class ServerInfoModel
  {
    public bool IsAccepted { get; set; }

    public int MaxMessageLength { get; set; }

    public byte[]? MethodSignature { get; set; }
  }
}
