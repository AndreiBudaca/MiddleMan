namespace MiddleMan.Service.WebSocketClients.Dto
{
  public class WebSocketClientDataDto
  {
    public string? ConnectionId { get; set; }

    public List<string> Methods { get; set; } = [];
  }
}
