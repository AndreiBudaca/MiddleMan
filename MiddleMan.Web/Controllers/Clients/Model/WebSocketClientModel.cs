namespace MiddleMan.Web.Controllers.Clients.Model
{
  public class WebSocketClientModel
  {
    public string? Name { get; set; }

    public string? MethodsUrl { get; set; }

    public bool IsConnected { get; set; }

    public string? Signature { get; set; }

    public string? TokenHash { get; set; }
  }
}
