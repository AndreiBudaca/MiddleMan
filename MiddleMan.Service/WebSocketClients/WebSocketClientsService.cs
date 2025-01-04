
using MiddleMan.Data.Redis;
using MiddleMan.Service.WebSocketClients.Dto;

namespace MiddleMan.Service.WebSocketClients
{
  public class WebSocketClientsService(IRedisContext redisContext) : IWebSocketClientsService
  {
    private const string BASE_KEY = "WebSocketClients";
    private readonly IRedisContext redisContext = redisContext;

    public async Task AddWebSocketClient(string identifier, string name, WebSocketClientDataDto clientData)
    {
      await redisContext.AddToHash(WebSocketClientKey(identifier), name, clientData);
    }

    public async Task DeleteWebSocketClient(string identifier, string name)
    {
      await redisContext.RemoveFromHash(WebSocketClientKey(identifier), name);
    }

    public async Task<bool> ExistsWebSocketClient(string identifier, string name)
    {
      return await redisContext.ExistsInHash(WebSocketClientKey(identifier), name);
    }

    public async Task<WebSocketClientDto?> GetWebSocketClient(string identifier, string name)
    {
      var clientData = await redisContext.GetFromHash<WebSocketClientDataDto>(WebSocketClientKey(identifier), name);
      if (clientData == null) return null;

      return new WebSocketClientDto(name, clientData);
    }

    public async Task<List<WebSocketClientDto>> GetWebSocketClients(string identifier)
    {
      var entries = await redisContext.GetAllFromHash<WebSocketClientDataDto>(WebSocketClientKey(identifier));

      return entries.Select(entry => new WebSocketClientDto(entry.Key, entry.Value)).ToList();
    }

    private static string WebSocketClientKey(string id) => $"{BASE_KEY}-{id}";
  }
}
