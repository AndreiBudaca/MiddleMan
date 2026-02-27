using MiddleMan.Service.WebSocketClientConnections.Classes;
using MiddleMan.Service.WebSocketClientConnections.ConnectionPicker;

namespace MiddleMan.Service.WebSocketClientConnections
{
  public interface IWebSocketClientConnectionsService
  {
    Task<List<string>> GetConnectedWebSocketClientConnections(string identifier);
    Task<bool> ExistsWebSocketClientConnection(string identifier, string name);
    Task<ClientConnection> GetWebSocketClientConnection(string identifier, string name, IConnectionPickerStrategy? connectionPickerStrategy = null);
    Task AddWebSocketClientConnection(string identifier, string name, string connectionId);
    Task<bool> AddWebSockerClientConnectionCapabilities(string identifier, string name, ClientCapabilities capabilities);
    Task DeleteWebSocketClientConnection(string identifier, string name);
    Task DeleteWebSocketClientConnection(string identifier, string namem, string connectionId);
  }
}