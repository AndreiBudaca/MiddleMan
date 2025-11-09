namespace MiddleMan.Service.WebSocketClients.Dto
{
  public class WebSocketClientDetailsDto : WebSocketClientDto
  {
    public string? MethodsUrl { get; set; }

    public bool IsConnected { get; set; }
  }
}
