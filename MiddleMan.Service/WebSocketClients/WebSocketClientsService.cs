using MiddleMan.Data.Persistency;
using MiddleMan.Data.Persistency.Classes;
using MiddleMan.Data.Persistency.Entities;
using MiddleMan.Service.WebSocketClientConnections;
using MiddleMan.Service.WebSocketClients.Dto;
using System.Security.Cryptography;
using System.Text;

namespace MiddleMan.Service.WebSocketClients
{
  public class WebSocketClientsService(IClientRepository clientRepository, IWebSocketClientConnectionsService webSocketClientConnectionsService) : IWebSocketClientsService
  {
    private readonly IWebSocketClientConnectionsService webSocketClientConnectionsService = webSocketClientConnectionsService;
    private readonly IClientRepository clientRepository = clientRepository;

    public async Task<WebSocketClientDto> AddWebSocketClient(NewWebSockerClientDto newClient)
    {
      var client = new Client
      {
        UserId = newClient.Identifier,
        Name = newClient.Name,
      };

      await clientRepository.AddAsync(client);

      return BuildDto(client);
    }

    public Task<bool> ExistsWebSocketClients(string identifier, string name)
    {
      return clientRepository.ExistsAsync((identifier, name));
    }

    public async Task<WebSocketClientDto?> GetWebSocketClient(string identifier, string name)
    {
      var clientData = await clientRepository.GetByIdAsync((identifier, name));

      if (clientData == null) return null;

      var isConnected = await webSocketClientConnectionsService.ExistsWebSocketClientConnection(identifier, name);
      return BuildDto(clientData, isConnected);
    }

    public async Task<IEnumerable<WebSocketClientDto>> GetWebSocketClients(string identifier)
    {
      var clientData = await clientRepository.GetByConditions(
      [
        new ColumnInfo
        {
          ColumnName = Client.Columns.UserId,
          Value = identifier,
        }
      ]);

      var connectedClients = await webSocketClientConnectionsService.GetConnectedWebSocketClientConnections(identifier);
      return clientData.Select(x => BuildDto(x, connectedClients.Any(c => c == x.Name)));
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

    public async Task DeleteWebSocketClient(string identifier, string name)
    {
      await clientRepository.DeleteAsync((identifier, name));
      await webSocketClientConnectionsService.DeleteWebSocketClientConnection(identifier, name);
    }

    public async Task<bool> IsValidWebSocketClientToken(string identifier, string name, string token)
    {
      var client = await clientRepository.GetByIdAsync((identifier, name));
      if (client == null || client.TokenHash == null) return false;

      var tokenHash = SHA256.HashData(Encoding.ASCII.GetBytes(token));
      return tokenHash.SequenceEqual(client.TokenHash);
    }

    private static WebSocketClientDto BuildDto(Client client, bool isConnected = false)
    {
      return new WebSocketClientDto
      {
        Name = client.Name,
        IsConnected = isConnected,
        MethodsUrl = client.MethodInfoUrl,
        Signature = client.Signatures,
        TokenHash = client.TokenHash,
      };
    }
  }
}
