using MiddleMan.Data.InMemory;
using MiddleMan.Data.Persistency;
using MiddleMan.Data.Persistency.ConnectionFactory;
using MiddleMan.Service.Blobs;
using MiddleMan.Service.WebSocketClientMethods;
using MiddleMan.Service.WebSocketClientConnections;
using MiddleMan.Service.WebSocketClients;
using MiddleMan.Core;
using MiddleMan.Communication;
using MiddleMan.Communication.Channels;
using MiddleMan.Service.Blobs.OracleObjectStorage;

namespace MiddleMan.Web.Infrastructure.Configuration
{
  public static class ServicesConfiguration
  {
    public static void AddServices(this IServiceCollection services)
    {
      // Add DB context
      services.AddSingleton<IInMemoryContext, PureInMemoryContext>();
      services.AddScoped<IDbConnectionFactory, MySQLConnectionFactory>(service =>
        new MySQLConnectionFactory(service.GetRequiredService<IConfiguration>().GetConnectionString(ConfigurationConstants.ConnectionStrings.MySql)!));

      // Repositories
      services.AddScoped<IClientRepository, ClientRepository>();

      // Add services
      services.AddScoped(service =>
        new OracleObjectStorageClientConfigurator(
          service.GetRequiredService<IConfiguration>().GetValue<string>(ConfigurationConstants.OracleCloud.User)!,
          service.GetRequiredService<IConfiguration>().GetValue<string>(ConfigurationConstants.OracleCloud.Fingerprint)!,
          service.GetRequiredService<IConfiguration>().GetValue<string>(ConfigurationConstants.OracleCloud.Tenancy)!,
          service.GetRequiredService<IConfiguration>().GetValue<string>(ConfigurationConstants.OracleCloud.Region)!,
          service.GetRequiredService<IConfiguration>().GetValue<string>(ConfigurationConstants.OracleCloud.KeyFile)!,
          service.GetRequiredService<IConfiguration>().GetValue<string>(ConfigurationConstants.OracleCloud.Namespace)!,
          service.GetRequiredService<IConfiguration>().GetValue<string>(ConfigurationConstants.OracleCloud.Bucket)!
        )
      );
      services.AddScoped<IBlobService, OracleObjectStorageBlobService>();
      services.AddScoped<IWebSocketClientsService, WebSocketClientsService>();
      services.AddScoped<IWebSocketClientMethodService, WebSocketClientMethodService>();
      services.AddScoped<IWebSocketClientConnectionsService, WebSocketClientConnectionsService>();

      // Add cummunication channels
      if (ServerCapabilities.ClusterMode)
      {
        services.AddSingleton<ICommunicationChannel, RedisCommunicationChannel>(service =>
          new RedisCommunicationChannel(
            service.GetRequiredService<IConfiguration>().GetConnectionString(ConfigurationConstants.ConnectionStrings.Redis)!,
            service.GetRequiredService<IInMemoryContext>())
        );
      }
      else
      {
        services.AddSingleton<ICommunicationChannel, DeadCommunicationChannel>();
      }

      // Add communication hubs
      services.AddScoped<StreamingCommunicationManager>();
      services.AddScoped<IntraServerCommunicationManager>();
      services.AddScoped<ClientInfoCommunicationManager>();
    }
  }
}
