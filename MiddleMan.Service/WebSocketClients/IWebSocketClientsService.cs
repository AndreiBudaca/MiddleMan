using MiddleMan.Service.WebSocketClients.Dto;

namespace MiddleMan.Service.WebSocketClients
{
  public interface IWebSocketClientsService
  {
    Task<bool> ExistsWebSocketClient(string identifier, string name);

    Task AddWebSocketClient(string identifier, string name, WebSocketClientDataDto clientData);

    Task DeleteWebSocketClient(string identifier, string name);

    Task<List<WebSocketClientDto>> GetWebSocketClients(string identifier);

    Task<WebSocketClientDto?> GetWebSocketClient(string identifier, string name);
  }
}
