using Microsoft.AspNetCore.Http.Connections;
using MiddleMan.Core;
using MiddleMan.Web.Hubs;

namespace MiddleMan.Web.Infrastructure.Configuration
{
  public static class HubsMapper
  {
    public static void MapHubs(this WebApplication app)
    {
      app.MapHub<PlaygroundHub>("/playground", options =>
      {
        options.TransportMaxBufferSize = ServerCapabilities.MaxChunkSize;
        options.ApplicationMaxBufferSize = ServerCapabilities.MaxChunkSize;
        options.Transports = HttpTransportType.WebSockets;
        options.AllowStatefulReconnects = false;
        options.CloseOnAuthenticationExpiration = true;
      });
    }
  }
}
