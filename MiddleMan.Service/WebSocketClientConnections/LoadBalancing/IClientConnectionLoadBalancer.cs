using MiddleMan.Service.WebSocketClientConnections.Classes;

namespace MiddleMan.Service.WebSocketClientConnections.LoadBalancing
{
  public interface IClientConnectionLoadBalancer
  {
    ClientConnectionsMetadata? PickClientConnection(ClientConnections clientConnections);
  }
}