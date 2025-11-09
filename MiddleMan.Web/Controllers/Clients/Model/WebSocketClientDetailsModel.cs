namespace MiddleMan.Web.Controllers.Clients.Model
{
  public class WebSocketClientDetailsModel : WebSocketClientModel
  {
    public string? MethodsUrl { get; set; }

    public bool IsConnected { get; set; }
  }
}
