using MiddleMan.Service.WebSocketClients.Dto;

namespace MiddleMan.Service.WebSocketClients
{
  public interface IWebSocketClientsService
  {
    Task AddWebSocketClientConnection(string identifier, string name, WebSocketClientConnectionDataDto clientData);

    Task<WebSocketClientDto> AddWebSocketClient(NewWebSockerClientDto newClient);

    Task<bool> ExistsWebSocketClients(string identifier, string name);

    Task<IEnumerable<WebSocketClientDto>> GetWebSocketClients(string identifier);

    Task<WebSocketClientConnectionDataDto?> GetWebSocketClientConnection(string identifier, string name);

    Task<WebSocketClientDto?> GetWebSocketClient(string identifier, string name);

    Task<byte[]?> UpdateWebSocketClientToken(string identifier, string name, string? token);

    Task<bool> IsValidWebSocketClientToken(string identifier, string name, string token);

    Task DeleteWebSocketClient(string identifier, string name);

    Task DeleteWebSocketClientConnection(string identifier, string name);
  }
}
