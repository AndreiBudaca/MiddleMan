namespace MiddleMan.Web.Controllers.WebSockets.Model
{
  public class WebSocketClientMethodModel
  {
    public string? Name { get; set; }

    public List<WebSocketClientMethodArgumentModel> Arguments { get; set; } = [];

    public WebSocketClientMethodArgumentModel? Returns { get; set; }
  }
}
