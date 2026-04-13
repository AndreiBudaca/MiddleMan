using MiddleMan.Data.InMemory;
using MiddleMan.Service.WebSocketClientConnections.Classes;
using MiddleMan.Service.WebSocketClientConnections.LoadBalancing;

namespace MiddleMan.Service.WebSocketClientConnections
{
  public class WebSocketClientConnectionsService(IInMemoryContext inMemoryContext) : IWebSocketClientConnectionsService
  {
    private readonly IInMemoryContext instanceInMemoryContext = inMemoryContext;
    private readonly IClientConnectionLoadBalancer loadBalancer = new RoundRobinClientLoadBalancer();
    private static readonly object globalLock = new();

    private static string WebSocketConnectionsKey(string identifier) => $"WebSocketClientConnections:{identifier}";

    public bool ExistsWebSocketClientConnection(string identifier, string name, string clientId)
    {
      lock (globalLock)
      {
        var clientConnections = instanceInMemoryContext.GetFromHash<ClientConnections>(WebSocketConnectionsKey(identifier), name);
        return clientConnections?.ConnectionsMetadata.ContainsKey(clientId) ?? false;
      }
    }

    public int AddWebSocketClientConnection(string identifier, string name, string clientId, string connectionId)
    {
      lock (globalLock)
      {
        var clientConnections = instanceInMemoryContext.GetFromHash<ClientConnections>(WebSocketConnectionsKey(identifier), name);

        if (clientConnections == null)
        {
          clientConnections = new ClientConnections
          {
            ConnectionsMetadata = new Dictionary<string, ClientConnectionsMetadata>
            {
              [clientId] = new ClientConnectionsMetadata
              {
                ConnectionId = connectionId,
              }
            }
          };

          instanceInMemoryContext.AddToHash(WebSocketConnectionsKey(identifier), name, clientConnections);
          return 1;
        }

        if (clientConnections.ConnectionsMetadata.TryGetValue(clientId, out ClientConnectionsMetadata? value))
        {
          value.ConnectionId = connectionId;
          value.Capabilities = null;
        }
        else
        {
          clientConnections.ConnectionsMetadata.Add(clientId, new ClientConnectionsMetadata
          {
            ConnectionId = connectionId,
          });
        }
        return clientConnections.ConnectionsMetadata.Count;
      }
    }

    public ClientConnection? GetWebSocketClientConnection(string identifier, string name)
    {
      lock (globalLock)
      {
        var clientConnections = instanceInMemoryContext.GetFromHash<ClientConnections>(WebSocketConnectionsKey(identifier), name);
        if (clientConnections == null) return null;

        var loadBalancingMetadata = clientConnections.LoadBalancingMetadata;

        var selectedConnection = loadBalancer.PickClientConnection(clientConnections);
        if (selectedConnection == null) return null;

        return new ClientConnection
        {
          ConnectionId = selectedConnection.ConnectionId,
          SameServerConnection = true,
          ClientCapabilities = selectedConnection.Capabilities ?? new()
        };
      }
    }

    public int DeleteWebSocketClientConnection(string identifier, string name, string clientId)
    {
      lock (globalLock)
      {
        var clientConnections = instanceInMemoryContext.GetFromHash<ClientConnections>(WebSocketConnectionsKey(identifier), name);
        if (clientConnections == null) return 0;

        if (!clientConnections.ConnectionsMetadata.ContainsKey(clientId)) return clientConnections.ConnectionsMetadata.Count;

        clientConnections.ConnectionsMetadata.Remove(clientId);
        return clientConnections.ConnectionsMetadata.Count;
      }
    }

    public void DeleteWebSocketClientConnection(string identifier, string name)
    {
      lock (globalLock)
      {
        instanceInMemoryContext.RemoveFromHash(WebSocketConnectionsKey(identifier), name);
      }
    }

    public void AddWebSockerClientConnectionCapabilities(string identifier, string name, string clientId, ClientCapabilities capabilities)
    {
      lock (globalLock)
      {
        var clientConnections = instanceInMemoryContext.GetFromHash<ClientConnections>(WebSocketConnectionsKey(identifier), name);
        if (clientConnections == null) return;

        var connection = clientConnections.ConnectionsMetadata.GetValueOrDefault(clientId);
        if (connection == null) return;

        connection.Capabilities = capabilities;
      }
    }
  }
}