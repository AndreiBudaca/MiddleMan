namespace MiddleMan.Web.Controllers.Clients.Model
{
  public class WebSocketClientConnectionStatusModel
  {
    public string? UserId { get; set; }

    public string? Name { get; set; }

    public bool IsConnected { get; set; }
  }
}