namespace MiddleMan.Service.WebSocketClientMethods.Classes
{
  public class MethodMetadataDto
  {
    public required byte Version { get; set; }
    public required byte Operation { get; set; }
    public required byte[] Signature { get; set; }
    public required byte MethodCount { get; set; }
  }
}
