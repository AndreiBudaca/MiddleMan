using MiddleMan.Data.InMemory;
using MiddleMan.Data.Persistency;
using MiddleMan.Data.Persistency.ConnectionFactory;
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
      services.AddScoped<IInMemoryContext, PureInMemoryContext>();
      services.AddScoped<IDbConnectionFactory, SqliteConnectionFactory>(service => new SqliteConnectionFactory(service.GetRequiredService<IConfiguration>().GetConnectionString("Sqlite")!));

      // Add services
      services.AddScoped<IClientRepository, ClientRepository>();
      services.AddScoped<IBlobService, LocalFileSystemBlobService>();
      services.AddScoped<IWebSocketClientsService, WebSocketClientsService>();
      services.AddScoped<IWebSocketClientMethodService, WebSocketClientMethodService>();
    }

    public static void InitializeDb(this WebApplication app)
    {
      using var scope = app.Services.CreateScope();
      var dbConnectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
      SqliteDatabaseInitializer.Initialize(dbConnectionFactory);
    }
  }
}
