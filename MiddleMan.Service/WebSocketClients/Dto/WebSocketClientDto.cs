namespace MiddleMan.Service.WebSocketClients.Dto
{
  public class WebSocketClientDto : WebSocketClientDataDto
  {
    public string? Name { get; set; }

    public WebSocketClientDto() { }

    public WebSocketClientDto(string name, WebSocketClientDataDto? clientData) 
    {
      Name = name;
      ConnectionId = clientData?.ConnectionId;
      Methods = clientData?.Methods ?? [];
    }
  }
}
