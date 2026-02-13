namespace MiddleMan.Service.WebSocketClients.Dto
{
  public class WebSocketClientDto
  {
    public string? Name { get; set; }

    public bool IsConnected { get; set; }

    public string? MethodsUrl { get; set; }

    public byte[]? Signature { get; set; }

    public byte[]? TokenHash { get; set; }
  }
}
