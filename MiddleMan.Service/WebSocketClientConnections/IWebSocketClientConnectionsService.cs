using MiddleMan.Service.WebSocketClientConnections.Classes;

namespace MiddleMan.Service.WebSocketClientConnections
{
  public interface IWebSocketClientConnectionsService
  {
    bool ExistsWebSocketClientConnection(string identifier, string name, string clientId);
    ClientConnection? GetWebSocketClientConnection(string identifier, string name);
    int AddWebSocketClientConnection(string identifier, string name, string clientId, string connectionId);
    void AddWebSockerClientConnectionCapabilities(string identifier, string name, string clientId, ClientCapabilities capabilities);
    void DeleteWebSocketClientConnection(string identifier, string name);
    int DeleteWebSocketClientConnection(string identifier, string name, string clientId);
  }
}