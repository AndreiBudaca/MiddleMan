using MiddleMan.Service.WebSocketClientConnections.Classes;

namespace MiddleMan.Service.WebSocketClientConnections
{
  public interface IWebSocketClientConnectionsService
  {
    Task<bool> ExistsWebSocketClientConnection(string identifier, string name);
    Task<ClientConnection> GetWebSocketClientConnection(string identifier, string name);
    Task AddWebSocketClientConnection(string identifier, string name, string connectionId);
    Task AddWebSockerClientConnectionCapabilities(string identifier, string name, string connectionId, ClientCapabilities capabilities);
    Task DeleteWebSocketClientConnection(string identifier, string name);
    Task DeleteWebSocketClientConnection(string identifier, string namem, string connectionId);
  }
}