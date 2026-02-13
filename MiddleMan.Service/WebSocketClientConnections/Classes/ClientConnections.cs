namespace MiddleMan.Service.WebSocketClientConnections
{
  public class ClientConnections
  {
    public List<string> ConnectionIds { get; set; } = [];
    
    public ClientConnectionsMetadata? Metadata { get; set; }
  }
}