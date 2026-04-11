namespace MiddleMan.Service.WebSocketClientConnections.Classes
{
  public class ClientConnections
  {
    public ClientLoadBalancingMetadata LoadBalancingMetadata { get; set; } = new();
    public Dictionary<string, ClientConnectionsMetadata> ConnectionsMetadata { get; set; } = [];
  }
}