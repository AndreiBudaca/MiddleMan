using MiddleMan.Data.InMemory;
using MiddleMan.Service.Blobs;
using MiddleMan.Service.WebSocketClientMethods;
using MiddleMan.Service.WebSocketClients;

namespace MiddleMan.Web.Infrastructure.Configuration
{
  public static class ServicesConfiguration
  {
    public static void AddServices(this IServiceCollection services)
    {
      // Add DB context
      services.AddScoped<IInMemoryContext, RedisContext>();

      // Add services
      services.AddScoped<IBlobService, LocalFileSystemBlobService>();
      services.AddScoped<IWebSocketClientsService, WebSocketClientsService>();
      services.AddScoped<IWebSocketClientMethodService, WebSocketClientMethodService>();
    }
  }
}
