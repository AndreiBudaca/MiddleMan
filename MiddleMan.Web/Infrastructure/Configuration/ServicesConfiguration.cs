using MiddleMan.Data.InMemory;
using MiddleMan.Data.Persistency;
using MiddleMan.Data.Persistency.ConnectionFactory;
using MiddleMan.Service.Blobs;
using MiddleMan.Service.WebSocketClientMethods;
using MiddleMan.Service.WebSocketClientConnections;
using MiddleMan.Service.WebSocketClients;
using MiddleMan.Web.Communication;
using MiddleMan.Core;
using MiddleMan.Service.WebSocketClientInvocationSession;

namespace MiddleMan.Web.Infrastructure.Configuration
{
  public static class ServicesConfiguration
  {
    public static void AddServices(this IServiceCollection services)
    {
      // Add DB context
      services.AddSingleton<IInMemoryContext, PureInMemoryContext>();
      services.AddSingleton<ISharedInMemoryContext, RedisContext>(service =>
        new RedisContext(service.GetRequiredService<IConfiguration>().GetConnectionString(ConfigurationConstants.ConnectionStrings.Redis)!));
      services.AddScoped<IDbConnectionFactory, MySQLConnectionFactory>(service => 
        new MySQLConnectionFactory(service.GetRequiredService<IConfiguration>().GetConnectionString(ConfigurationConstants.ConnectionStrings.MySql)!));

      // Repositories
      services.AddScoped<IClientRepository, ClientRepository>();
      
      // Add services
      services.AddScoped<IBlobService, LocalFileSystemBlobService>();
      services.AddScoped<IWebSocketClientsService, WebSocketClientsService>();
      services.AddScoped<IWebSocketClientMethodService, WebSocketClientMethodService>();
      services.AddScoped<IWebSocketClientConnectionsService, WebSocketClientConnectionsService>();
      services.AddScoped<IWebSocketClientInvocationSessionService, WebSocketClientInvocationSessionService>();

      // Add communication hub
      services.AddSingleton<StreamingCommunicationManager>();
    }
  }
}
