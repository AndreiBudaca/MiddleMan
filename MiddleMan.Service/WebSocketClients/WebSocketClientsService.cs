using MiddleMan.Data.Persistency;
using MiddleMan.Data.Persistency.Classes;
using MiddleMan.Data.Persistency.Entities;
using MiddleMan.Service.Blobs;
using MiddleMan.Service.WebSocketClients.Dto;
using System.Security.Cryptography;
using System.Text;

namespace MiddleMan.Service.WebSocketClients
{
  public class WebSocketClientsService(IClientRepository clientRepository, IClientShareRepository clientShareRepository, IBlobService blobService) : IWebSocketClientsService
  {
    private readonly IBlobService blobService = blobService;
    private readonly IClientRepository clientRepository = clientRepository;
    private readonly IClientShareRepository clientShareRepository = clientShareRepository;

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

    public async Task AddWebSocketClientShare(string identifier, string name, string sharedWithUserEmail)
    {
      var clientShare = new ClientShare
      {
        UserId = identifier,
        Name = name,
        SharedWithUserEmail = sharedWithUserEmail,
      };

      await clientShareRepository.AddAsync(clientShare);
    }

    public Task<bool> ExistsWebSocketClients(string identifier, string name)
    {
      return clientRepository.ExistsAsync((identifier, name));
    }

    public Task<bool> ExistsWebSocketClientShares(string identifier, string name, string sharedWithUserEmail)
    {
      return clientShareRepository.ExistsAsync((identifier, name, sharedWithUserEmail));
    }

    public async Task<WebSocketClientDto?> GetWebSocketClient(string identifier, string name)
    {
      var clientData = await clientRepository.GetByIdAsync((identifier, name));

      if (clientData == null) return null;

      return await BuildDto(clientData);
    }

    public async Task<IEnumerable<WebSocketClientDto>> GetWebSocketClients(string identifier, string email, bool onlyOwned = false)
    {
      var clientData = await clientRepository.GetByConditions(
      [
        new ColumnInfo
        {
          ColumnName = Client.Columns.UserId,
          Value = identifier,
        }
      ]);

      var ownedClientShares = await clientShareRepository.GetByConditions(
      [
        new ColumnInfo
        {
          ColumnName = ClientShare.Columns.UserId,
          Value = identifier,
        }
      ]);

      var ownedClientSharesLookup = ownedClientShares
        .GroupBy(cs => cs.Name)
        .ToDictionary(
          group => group.Key,
          group => group
            .Select(cs => cs.SharedWithUserEmail)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray());

      var sharedClients = Enumerable.Empty<Client>();
      if (!onlyOwned)
      {
        var sharedClientData = await clientShareRepository.GetByConditions(
        [
          new ColumnInfo
          {
            ColumnName = ClientShare.Columns.SharedWithUserEmail,
            Value = email,
          }
        ]);

        var sharedClientKeys = sharedClientData.Select(cs => (cs.UserId, cs.Name));
        sharedClients = await clientRepository.GetByIds(sharedClientKeys);
      }

      var clients = new List<WebSocketClientDto>();
      foreach (var client in clientData)
      {
        var sharedWithUserEmails = ownedClientSharesLookup.TryGetValue(client.Name, out var emails)
          ? emails
          : [];

        clients.Add(await BuildDto(client, sharedWithUserEmails));
      }

      foreach (var client in sharedClients)
      {
        clients.Add(await BuildDto(client, []));
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

    public async Task DeleteWebSocketClientShare(string identifier, string name, string sharedWithUserEmail)
    {
      await clientShareRepository.DeleteAsync((identifier, name, sharedWithUserEmail));
    }

    public async Task<bool> IsValidWebSocketClientToken(string identifier, string name, string token)
    {
      var client = await clientRepository.GetByIdAsync((identifier, name));
      if (client == null || client.TokenHash == null) return false;

      var tokenHash = SHA256.HashData(Encoding.ASCII.GetBytes(token));
      return tokenHash.SequenceEqual(client.TokenHash);
    }

    private async Task<WebSocketClientDto> BuildDto(Client client, IEnumerable<string>? sharedWithUserEmails = null)
    {
      var methodsUrl = string.IsNullOrWhiteSpace(client.MethodInfoUrl) ? null : await blobService.GetAbsoluteUrl(client.MethodInfoUrl);
      return new WebSocketClientDto
      {
        UserId = client.UserId,
        Name = client.Name,
        MethodsUrl = methodsUrl,
        Signature = client.Signatures,
        TokenHash = client.TokenHash,
        SharedWithUserEmails = sharedWithUserEmails ?? [],
      };
    }
  }
}
