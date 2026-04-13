using MiddleMan.Data.Persistency;
using MiddleMan.Data.Persistency.Classes;
using MiddleMan.Data.Persistency.Entities;
using MiddleMan.Service.Blobs;
using MiddleMan.Service.WebSocketClients.Dto;
using System.Security.Cryptography;
using System.Text;

namespace MiddleMan.Service.WebSocketClients
{
  public class WebSocketClientsService(IClientRepository clientRepository, IBlobService blobService) : IWebSocketClientsService
  {
    private readonly IBlobService blobService = blobService;
    private readonly IClientRepository clientRepository = clientRepository;

    public async Task<WebSocketClientDto> AddWebSocketClient(NewWebSockerClientDto newClient)
    {
      var client = new Client
      {
        UserId = newClient.Identifier,
        Name = newClient.Name,
      };

      await clientRepository.AddAsync(client);

      return await BuildDto(client);
    }

    public Task<bool> ExistsWebSocketClients(string identifier, string name)
    {
      return clientRepository.ExistsAsync((identifier, name));
    }

    public async Task<WebSocketClientDto?> GetWebSocketClient(string identifier, string name)
    {
      var clientData = await clientRepository.GetByIdAsync((identifier, name));

      if (clientData == null) return null;

      return await BuildDto(clientData);
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

      var clients = new List<WebSocketClientDto>();
      foreach (var client in clientData)
      {
        clients.Add(await BuildDto(client));
      }

      return clients;
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
      // TO DO
      // await webSocketClientConnectionsService.DeleteWebSocketClientConnection(identifier, name);
    }

    public async Task<bool> IsValidWebSocketClientToken(string identifier, string name, string token)
    {
      var client = await clientRepository.GetByIdAsync((identifier, name));
      if (client == null || client.TokenHash == null) return false;

      var tokenHash = SHA256.HashData(Encoding.ASCII.GetBytes(token));
      return tokenHash.SequenceEqual(client.TokenHash);
    }

    private async Task<WebSocketClientDto> BuildDto(Client client)
    {
      var methodsUrl = string.IsNullOrWhiteSpace(client.MethodInfoUrl) ? null : await blobService.GetAbsoluteUrl(client.MethodInfoUrl);
      return new WebSocketClientDto
      {
        Name = client.Name,
        MethodsUrl = methodsUrl,
        Signature = client.Signatures,
        TokenHash = client.TokenHash,
      };
    }
  }
}
