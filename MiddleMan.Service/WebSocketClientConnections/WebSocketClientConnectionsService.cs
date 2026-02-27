using MiddleMan.Data.InMemory;
using MiddleMan.Service.WebSocketClientConnections.Classes;
using MiddleMan.Service.WebSocketClientConnections.ConnectionPicker;

namespace MiddleMan.Service.WebSocketClientConnections
{
  public class WebSocketClientConnectionsService(IInMemoryContext inMemoryContext) : IWebSocketClientConnectionsService
  {
    private readonly IInMemoryContext inMemoryContext = inMemoryContext;

    public async Task<List<string>> GetConnectedWebSocketClientConnections(string identifier)
    {
      var existingConnections = await inMemoryContext.GetAllFromHash<ClientConnections>(WebSocketClientKey(identifier));
      return [.. existingConnections.Keys];
    }

    public async Task<bool> ExistsWebSocketClientConnection(string identifier, string name)
    {
      var existingConnection = await inMemoryContext.GetFromHash<ClientConnections>(WebSocketClientKey(identifier), name);
      return existingConnection?.ConnectionIds.Count > 0;
    }

    public async Task AddWebSocketClientConnection(string identifier, string name, string connectionId)
    {
      var existingConnection = await inMemoryContext.GetFromHash<ClientConnections>(WebSocketClientKey(identifier), name);
      existingConnection ??= new ClientConnections
      {
        ConnectionIds = [],
        Metadata = new ClientConnectionsMetadata
        {
          LastPickedIndex = 0,
        }
      };

      existingConnection.ConnectionIds.Add(connectionId);
      await inMemoryContext.AddToHash(WebSocketClientKey(identifier), name, existingConnection);
    }

    public async Task<ClientConnection> GetWebSocketClientConnection(string identifier, string name, IConnectionPickerStrategy? connectionPickerStrategy = null)
    {
      var existingConnection = await inMemoryContext.GetFromHash<ClientConnections>(WebSocketClientKey(identifier), name);
      if (existingConnection == null || existingConnection.ConnectionIds.Count == 0) return new ClientConnection();

      connectionPickerStrategy ??= IConnectionPickerStrategy.Default;
      int indexToPick = connectionPickerStrategy.PickAndUpdate(existingConnection);
      await inMemoryContext.AddToHash(WebSocketClientKey(identifier), name, existingConnection);

      return new ClientConnection
      {
        ConnectionId = existingConnection.ConnectionIds[indexToPick],
        ClientCapabilities = existingConnection.Metadata?.Capabilities ?? new ClientCapabilities(),
      };
    }

    public async Task DeleteWebSocketClientConnection(string identifier, string namem, string connectionId)
    {
      var existingConnection = await inMemoryContext.GetFromHash<ClientConnections>(WebSocketClientKey(identifier), namem);
      if (existingConnection == null) return;

      existingConnection.ConnectionIds.Remove(connectionId);
      if (existingConnection.ConnectionIds.Count == 0)
      {
        await inMemoryContext.RemoveFromHash(WebSocketClientKey(identifier), namem);
      }
      else
      {
        await inMemoryContext.AddToHash(WebSocketClientKey(identifier), namem, existingConnection);
      }
    }

    public Task DeleteWebSocketClientConnection(string identifier, string name)
    {
      return inMemoryContext.RemoveFromHash(WebSocketClientKey(identifier), name);
    }

    private static string WebSocketClientKey(string identifier) => $"WebSocketClientConnections:{identifier}";

    public async Task<bool> AddWebSockerClientConnectionCapabilities(string identifier, string name, ClientCapabilities capabilities)
    {
      var existingConnection = await inMemoryContext.GetFromHash<ClientConnections>(WebSocketClientKey(identifier), name);
      existingConnection ??= new ClientConnections();
      existingConnection.Metadata ??= new ClientConnectionsMetadata();

      if (existingConnection.Metadata.Capabilities != null && !capabilities.Equals(existingConnection.Metadata.Capabilities)) return false;

      existingConnection.Metadata.Capabilities = capabilities;
      await inMemoryContext.AddToHash(WebSocketClientKey(identifier), name, existingConnection);
      return true;
    }
  }
}