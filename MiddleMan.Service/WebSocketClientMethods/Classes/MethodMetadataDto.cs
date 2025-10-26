namespace MiddleMan.Service.WebSocketClientMethods.Classes
{
  public class MethodMetadataDto
  {
    public required byte Version { get; set; }
    public required byte Operation { get; set; }
    public required short MethodCount { get; set; }
  }
}
