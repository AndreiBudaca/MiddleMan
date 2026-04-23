using MiddleMan.Service.WebSocketClients.Dto;

namespace MiddleMan.Service.WebSocketClients
{
  public interface IWebSocketClientsService
  {
    Task<WebSocketClientDto> AddWebSocketClient(NewWebSockerClientDto newClient);

    Task AddWebSocketClientShare(string identifier, string name, string sharedWithUserEmail);

    Task<bool> ExistsWebSocketClients(string identifier, string name);

    Task<bool> ExistsWebSocketClientShares(string identifier, string name, string sharedWithUserEmail);

    Task<IEnumerable<WebSocketClientDto>> GetWebSocketClients(string identifier, string email, bool onlyOwned = false);

    Task<WebSocketClientDto?> GetWebSocketClient(string identifier, string name);

    Task<byte[]?> UpdateWebSocketClientToken(string identifier, string name, string? token);

    Task DeleteWebSocketClient(string identifier, string name);

    Task DeleteWebSocketClientShare(string identifier, string name, string sharedWithUserEmail);

    Task<bool> IsValidWebSocketClientToken(string identifier, string name, string token);
  }
}
