using MiddleMan.Service.WebSocketClientConnections.Classes;

namespace MiddleMan.Service.WebSocketClientConnections
{
  public interface IWebSocketClientConnectionsService
  {
    bool ExistsWebSocketClientConnection(string identifier, string name);
    ClientConnection? GetWebSocketClientConnection(string identifier, string name);
    int AddWebSocketClientConnection(string identifier, string name, string connectionId);
    void AddWebSockerClientConnectionCapabilities(string identifier, string name, string connectionId, ClientCapabilities capabilities);
    void DeleteWebSocketClientConnection(string identifier, string name);
    int DeleteWebSocketClientConnection(string identifier, string namem, string connectionId);
  }
}