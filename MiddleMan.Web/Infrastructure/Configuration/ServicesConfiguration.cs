using MiddleMan.Data.Redis;
using MiddleMan.Service.WebSocketClients;

namespace MiddleMan.Web.Infrastructure.Configuration
{
  public static class ServicesConfiguration
  {
    public static void AddServices(this IServiceCollection services)
    {
      // Add DB context
      services.AddScoped<IRedisContext, RedisContext>();

      // Add services
      services.AddScoped<IWebSocketClientsService, WebSocketClientsService>();
    }
  }
}
