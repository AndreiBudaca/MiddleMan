using MiddleMan.Service.WebSocketClients.Dto;

namespace MiddleMan.Service.WebSocketClients
{
  public interface IWebSocketClientsService
  {
    Task AddWebSocketClient(string identifier, string name, WebSocketClientConnectionDataDto clientData);
   
    Task<IEnumerable<WebSocketClientDetailsDto>> GetWebSocketClients(string identifier);
    
    Task<WebSocketClientConnectionDataDto?> GetWebSocketClientConnection(string identifier, string name);

    Task<WebSocketClientDetailsDto?> GetWebSocketClient(string identifier, string name);
    
    Task DeleteWebSocketClient(string identifier, string name);
  }
}
