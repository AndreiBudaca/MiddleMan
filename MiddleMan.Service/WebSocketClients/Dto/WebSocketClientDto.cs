namespace MiddleMan.Service.WebSocketClients.Dto
{
  public class WebSocketClientDto : WebSocketClientConnectionDataDto
  {
    public string? Name { get; set; }

    public WebSocketClientDto() { }

    public WebSocketClientDto(string name, WebSocketClientConnectionDataDto? clientData) 
    {
      Name = name;
      ConnectionId = clientData?.ConnectionId;
    }
  }
}
