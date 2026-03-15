using MiddleMan.Data.InMemory;
using MiddleMan.Service.WebSocketClientConnections.Classes;

namespace MiddleMan.Service.WebSocketClientConnections
{
  public class WebSocketClientConnectionsService(IInMemoryContext inMemoryContext) : IWebSocketClientConnectionsService
  {
    private readonly IInMemoryContext intanceInMemoryContext = inMemoryContext;
    private const string WebSocketCapabilitesHashKey = "WebSocketClientCapabilities";
    private static string WebSocketConnectionsKey(string identifier, string name) => $"WebSocketClientConnections:{identifier}:{name}";

    public bool ExistsWebSocketClientConnection(string identifier, string name)
    {
      var connectionCount = intanceInMemoryContext.ListCount(WebSocketConnectionsKey(identifier, name));
      return connectionCount > 0;
    }

    public int AddWebSocketClientConnection(string identifier, string name, string connectionId)
    {
      return intanceInMemoryContext.AddToList(WebSocketConnectionsKey(identifier, name), connectionId);
    }

    public ClientConnection? GetWebSocketClientConnection(string identifier, string name)
    {
      var clientConnection = intanceInMemoryContext.GetRandomFromList<string>(WebSocketConnectionsKey(identifier, name));

      if (clientConnection == null)
      {
        return null;
      }

      var capabilities = intanceInMemoryContext.GetFromHash<ClientCapabilities>(WebSocketCapabilitesHashKey, clientConnection) ??
       new ClientCapabilities();

      return new ClientConnection
      {
        ConnectionId = clientConnection,
        ClientCapabilities = capabilities ?? new ClientCapabilities(),
      };
    }

    public int DeleteWebSocketClientConnection(string identifier, string namem, string connectionId)
    {
      return intanceInMemoryContext.RemoveFromList(WebSocketConnectionsKey(identifier, namem), connectionId);
    }

    public void DeleteWebSocketClientConnection(string identifier, string name)
    {
      var connections = intanceInMemoryContext.GetAllFromList<string>(WebSocketConnectionsKey(identifier, name));
      foreach (var connection in connections)
      {
        if (connection == null) continue;
        intanceInMemoryContext.RemoveFromList(WebSocketConnectionsKey(identifier, name), connection);
      }
    }

    public void AddWebSockerClientConnectionCapabilities(string identifier, string name, string connectionId, ClientCapabilities capabilities)
    {
      intanceInMemoryContext.AddToHash(WebSocketCapabilitesHashKey, connectionId, capabilities);
    }
  }
}