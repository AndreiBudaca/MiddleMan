namespace MiddleMan.Service.WebSocketClients.Dto
{
  public class WebSocketClientDto
  {
    public string? UserId { get; set; }

    public string? Name { get; set; }

    public string? MethodsUrl { get; set; }

    public byte[]? Signature { get; set; }

    public byte[]? TokenHash { get; set; }

    public IEnumerable<string> SharedWithUserEmails { get; set; } = [];
  }
}
