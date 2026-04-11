using MiddleMan.Service.WebSocketClientConnections.Classes;

namespace MiddleMan.Service.WebSocketClientConnections.LoadBalancing
{
  public class RoundRobinClientLoadBalancer : IClientConnectionLoadBalancer
  {
    public ClientConnectionsMetadata? PickClientConnection(ClientConnections clientConnections)
    {
      if (clientConnections.ConnectionsMetadata.Count == 0) return null;

      var loadBalancingMetadata = clientConnections.LoadBalancingMetadata;
      var lastPicked = loadBalancingMetadata.LastPicked;
      var nextIndex = (lastPicked + 1) % clientConnections.ConnectionsMetadata.Count;

      loadBalancingMetadata.LastPicked = nextIndex;
      return clientConnections.ConnectionsMetadata.Values.ElementAt(nextIndex);
    }
  }
}