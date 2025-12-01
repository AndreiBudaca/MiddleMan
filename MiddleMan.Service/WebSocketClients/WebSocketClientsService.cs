using MiddleMan.Data.InMemory;
using MiddleMan.Data.Persistency;
using MiddleMan.Data.Persistency.Classes;
using MiddleMan.Data.Persistency.Entities;
using MiddleMan.Service.WebSocketClients.Dto;
using System.Security.Cryptography;
using System.Text;

namespace MiddleMan.Service.WebSocketClients
{
  public class WebSocketClientsService(IInMemoryContext memoryContext, IClientRepository clientRepository) : IWebSocketClientsService
  {
    private const string BASE_KEY = "WebSocketClients";
    private readonly IInMemoryContext memoryContext = memoryContext;
    private readonly IClientRepository clientRepository = clientRepository;

    public async Task AddWebSocketClientConnection(string identifier, string name, WebSocketClientConnectionDataDto clientData)
    {
      await clientRepository.UpdateAsync((identifier, name),
        [
          new ColumnInfo
          {
            ColumnName = Client.Columns.LastConnectedAt,
            Value = DateTime.Now,
          }
        ]);

      await memoryContext.AddToHash(WebSocketClientKey(identifier), name, clientData);
    }

    public async Task<WebSocketClientDto> AddWebSocketClient(NewWebSockerClientDto newClient)
    {
      var client = new Client
      {
        UserId = newClient.Identifier,
        Name = newClient.Name,
      };

      await clientRepository.AddAsync(client);

      return BuildDto(client, null);
    }

    public Task<bool> ExistsWebSocketClients(string identifier, string name)
    {
      return clientRepository.ExistsAsync((identifier, name));
    }

    public Task<WebSocketClientConnectionDataDto?> GetWebSocketClientConnection(string identifier, string name)
    {
      return memoryContext.GetFromHash<WebSocketClientConnectionDataDto>(WebSocketClientKey(identifier), name);
    }

    public async Task<WebSocketClientDto?> GetWebSocketClient(string identifier, string name)
    {
      var clientConnection = await memoryContext.GetFromHash<WebSocketClientConnectionDataDto>(WebSocketClientKey(identifier), name);
      var clientData = await clientRepository.GetByIdAsync((identifier, name));

      if (clientData == null) return null;
      return BuildDto(clientData, clientConnection);
    }

    public async Task<IEnumerable<WebSocketClientDto>> GetWebSocketClients(string identifier)
    {
      var clientConnections = await memoryContext.GetAllFromHash<WebSocketClientConnectionDataDto>(WebSocketClientKey(identifier));
      var clientData = await clientRepository.GetByConditions([new ColumnInfo
      {
        ColumnName = Client.Columns.UserId,
        Value = identifier,
      }]);

      return clientData.Select(clientData =>
      {
        _ = clientConnections.TryGetValue(clientData.Name, out var clientConnection);
        return BuildDto(clientData, clientConnection);
      });
    }
    
    public async Task<byte[]?> UpdateWebSocketClientToken(string identifier, string name, string? token)
    {
      var newToken = token != null ?
        SHA256.HashData(Encoding.ASCII.GetBytes(token)) :
        null;

      await clientRepository.UpdateAsync
      (
        (identifier, name),
        [
          new ColumnInfo
          {
            ColumnName = Client.Columns.TokenHash,
            Value = newToken,
          }
        ]
      );

      return newToken;
    }

    public async Task<bool> IsValidWebSocketClientToken(string identifier, string name, string token)
    {
      var client = await clientRepository.GetByIdAsync((identifier, name));
      if (client == null || client.TokenHash == null) return false;

      var tokenHash = SHA256.HashData(Encoding.ASCII.GetBytes(token));
      return tokenHash.SequenceEqual(client.TokenHash);
    }

    public async Task DeleteWebSocketClient(string identifier, string name)
    {
      await clientRepository.DeleteAsync((identifier, name));
      await DeleteWebSocketClientConnection(identifier, name);
    }
    
    public async Task DeleteWebSocketClientConnection(string identifier, string name)
    {
      await memoryContext.RemoveFromHash(WebSocketClientKey(identifier), name);

      await clientRepository.UpdateAsync
      (
        (identifier, name),
        [
          new ColumnInfo
          {
            ColumnName = Client.Columns.LastConnectedAt,
            Value = DateTime.Now,
          }
        ]
      );
    }

    private static string WebSocketClientKey(string id) => $"{BASE_KEY}-{id}";

    private static WebSocketClientDto BuildDto(Client client, WebSocketClientConnectionDataDto? connectionInfo)
    {
      return new WebSocketClientDto
      {
        Name = client.Name,
        ConnectionId = connectionInfo?.ConnectionId,
        MethodsUrl = client.MethodInfoUrl,
        LastConnectedAt = client.LastConnectedAt,
        Signature = client.Signatures,
        TokenHash = client.TokenHash,
      };
    }
  }
}
