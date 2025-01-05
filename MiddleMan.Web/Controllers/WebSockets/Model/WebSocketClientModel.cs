namespace MiddleMan.Web.Controllers.WebSockets.Model
{
  public class WebSocketClientModel
  {
    public string? Name { get; set; }

    public List<WebSocketClientMethodModel> Methods { get; set; } = [];
  }
}
