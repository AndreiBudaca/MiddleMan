namespace MiddleMan.Service.WebSocketClientConnections.Classes
{
  public class ClientConnectionsMetadata
  {
    public required string ConnectionId { get; set; }

    public ClientCapabilities? Capabilities { get; set; }
  }
}