namespace MiddleMan.Service.WebSocketClientConnections.Classes
{
  public class ClientConnection
  {
    public string? ConnectionId { get; set; }

    public bool IsConnectedToCurrentServer { get; set; }
    
    public ClientCapabilities ClientCapabilities { get; set; } = new();
  }
}