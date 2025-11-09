using MiddleMan.Data.InMemory;
using MiddleMan.Data.Persistance;
using MiddleMan.Data.Persistance.Classes;
using MiddleMan.Data.Persistance.Entities;
using MiddleMan.Service.WebSocketClients.Dto;
using System.Xml.Linq;

namespace MiddleMan.Service.WebSocketClients
{
  public class WebSocketClientsService(IInMemoryContext memoryContext, IClientRepository clientRepository) : IWebSocketClientsService
  {
    private const string BASE_KEY = "WebSocketClients";
    private readonly IInMemoryContext memoryContext = memoryContext;
    private readonly IClientRepository clientRepository = clientRepository;

    public async Task AddWebSocketClient(string identifier, string name, WebSocketClientConnectionDataDto clientData)
    {
      await memoryContext.AddToHash(WebSocketClientKey(identifier), name, clientData);

      var isExistingClient = await clientRepository.ExistsAsync((identifier, name));
      var client = new Client
      {
        IsConnected = true,
        LastConnectedAt = DateTime.UtcNow,
        Name = name,
        UserId = identifier,
      };

      if (isExistingClient)
      {
        await clientRepository.UpdateAsync((identifier, name),
          [
            new ColumnInfo
            {
              ColumnName = Client.Columns.IsConnected,
              Value = client.IsConnected
            },
            new ColumnInfo
            {
              ColumnName = Client.Columns.LastConnectedAt,
              Value = client.LastConnectedAt
            }
          ]);
      }
      else
      {
        await clientRepository.AddAsync(client);
      }
    }

    public async Task DeleteWebSocketClient(string identifier, string name)
    {
      await memoryContext.RemoveFromHash(WebSocketClientKey(identifier), name);

      await clientRepository.UpdateAsync((identifier, name),
         [
           new ColumnInfo
            {
              ColumnName = Client.Columns.IsConnected,
              Value = false
            },
         ]);
    }

    public Task<WebSocketClientConnectionDataDto?> GetWebSocketClientConnection(string identifier, string name)
    {
      return memoryContext.GetFromHash<WebSocketClientConnectionDataDto>(WebSocketClientKey(identifier), name);
    }

    public async Task<WebSocketClientDetailsDto?> GetWebSocketClient(string identifier, string name)
    {
      var clientConnection = await memoryContext.GetFromHash<WebSocketClientConnectionDataDto>(WebSocketClientKey(identifier), name);
      var clientData = await clientRepository.GetByIdAsync((identifier, name));

      if (clientData == null && clientData == null) return null;

      return new WebSocketClientDetailsDto
      {
        Name = name,
        ConnectionId = clientConnection?.ConnectionId,
        MethodsUrl = clientData?.MethodInfoUrl,
        IsConnected = (clientData?.IsConnected ?? false) && clientConnection is not null,
      };
    }

    public async Task<IEnumerable<WebSocketClientDetailsDto>> GetWebSocketClients(string identifier)
    {
      var clientconnections = await memoryContext.GetAllFromHash<WebSocketClientConnectionDataDto>(WebSocketClientKey(identifier));
      var clientData = await clientRepository.GetByContitions([new ColumnInfo
      {
        ColumnName = Client.Columns.UserId,
        Value = identifier,
      }]);

      return clientData.Select(clientData =>
      {
        clientconnections.TryGetValue(clientData.Name, out var clientConnection);

        return new WebSocketClientDetailsDto
        {
          Name = clientData.Name,
          ConnectionId = clientConnection?.ConnectionId,
          MethodsUrl = clientData?.MethodInfoUrl,
          IsConnected = (clientData?.IsConnected ?? false) && clientConnection is not null,
        };
      });
    }

    private static string WebSocketClientKey(string id) => $"{BASE_KEY}-{id}";
  }
}
