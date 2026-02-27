namespace MiddleMan.Service.WebSocketClientConnections.Classes
{
  public class ClientConnectionsMetadata
  {
    public int LastPickedIndex { get; set; } = 0;

    public ClientCapabilities? Capabilities { get; set; }
  }
}