namespace MiddleMan.Service.WebSocketClients.Dto
{
  public class WebSocketClientMethodDto
  {
    public string? Name { get; set; }

    public List<WebSocketClientMethodArgumentDto> Arguments { get; set; } = [];

    public WebSocketClientMethodArgumentDto? Returns { get; set; }
  }
}
