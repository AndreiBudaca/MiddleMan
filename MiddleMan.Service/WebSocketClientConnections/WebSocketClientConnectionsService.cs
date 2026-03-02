using MiddleMan.Data.InMemory;
using MiddleMan.Service.WebSocketClientConnections.Classes;

namespace MiddleMan.Service.WebSocketClientConnections
{
  public class WebSocketClientConnectionsService(IInMemoryContext inMemoryContext,
    ISharedInMemoryContext sharedInMemoryContext) : IWebSocketClientConnectionsService
  {
    private readonly IInMemoryContext intanceInMemoryContext = inMemoryContext;
    private readonly ISharedInMemoryContext sharedInMemoryContext = sharedInMemoryContext;
    private const string WebSocketCapabilitesHashKey = "WebSocketClientCapabilities";
    private static string WebSocketConnectionsKey(string identifier, string name) => $"WebSocketClientConnections:{identifier}:{name}";

    public async Task<bool> ExistsWebSocketClientConnection(string identifier, string name)
    {
      var connectionCount = await sharedInMemoryContext.ListCount(WebSocketConnectionsKey(identifier, name));
      return connectionCount > 0;
    }

    public async Task AddWebSocketClientConnection(string identifier, string name, string connectionId)
    {
      await intanceInMemoryContext.AddToList(WebSocketConnectionsKey(identifier, name), connectionId);
      await sharedInMemoryContext.AddToList(WebSocketConnectionsKey(identifier, name), connectionId);
    }

    public async Task<ClientConnection> GetWebSocketClientConnection(string identifier, string name)
    {
      var isConnectedToCurrentServer = true;
      var clientConnection = await intanceInMemoryContext.GetRandomFromList<string>(WebSocketConnectionsKey(identifier, name));
      
      if (clientConnection == null)
      {
        isConnectedToCurrentServer = false;
        clientConnection = await sharedInMemoryContext.GetRandomFromList<string>(WebSocketConnectionsKey(identifier, name));
      }

      if (clientConnection == null) return new ClientConnection();
      var capabilities = await sharedInMemoryContext.GetFromHash<ClientCapabilities>(WebSocketCapabilitesHashKey, clientConnection);
      
      return new ClientConnection
      {
        ConnectionId = clientConnection,
        IsConnectedToCurrentServer = isConnectedToCurrentServer,
        ClientCapabilities = capabilities ?? new ClientCapabilities(),
      };
    }

    public async Task DeleteWebSocketClientConnection(string identifier, string namem, string connectionId)
    {
      await sharedInMemoryContext.RemoveFromHash(WebSocketCapabilitesHashKey, connectionId);
      await intanceInMemoryContext.RemoveFromList(WebSocketConnectionsKey(identifier, namem), connectionId);
      await sharedInMemoryContext.RemoveFromList(WebSocketConnectionsKey(identifier, namem), connectionId);
    }

    public async Task DeleteWebSocketClientConnection(string identifier, string name)
    {
      var connections = await sharedInMemoryContext.GetAllFromList<string>(WebSocketConnectionsKey(identifier, name));
      
      foreach (var connection in connections)
      {
        if (connection == null) continue;

        await intanceInMemoryContext.RemoveFromList(WebSocketConnectionsKey(identifier, name), connection);
        await sharedInMemoryContext.RemoveFromList(WebSocketConnectionsKey(identifier, name), connection);
        await sharedInMemoryContext.RemoveFromHash(WebSocketCapabilitesHashKey, connection);
      }
    }

    public Task AddWebSockerClientConnectionCapabilities(string identifier, string name, string connectionId, ClientCapabilities capabilities)
    {
      return sharedInMemoryContext.AddToHash(WebSocketCapabilitesHashKey, connectionId, capabilities);
    }
  }
}