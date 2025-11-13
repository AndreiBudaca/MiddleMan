namespace MiddleMan.Service.WebSocketClients.Dto
{
  public class WebSocketClientDto : WebSocketClientConnectionDataDto
  {
    public string? Name { get; set; }

    public string? MethodsUrl { get; set; }

    public DateTime? LastConnectedAt { get; set; }

    public byte[]? Signature { get; set; }

    public byte[]? TokenHash { get; set; }
  }
}
