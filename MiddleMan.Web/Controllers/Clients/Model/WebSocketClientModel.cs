namespace MiddleMan.Web.Controllers.Clients.Model
{
  public class WebSocketClientModel
  {
    public string? UserId { get; set; }

    public string? Name { get; set; }

    public string? MethodsUrl { get; set; }

    public string? Signature { get; set; }

    public string? TokenHash { get; set; }

    public IEnumerable<string> SharedWithUserEmails { get; set; } = [];
  }
}
