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
        options.TransportMaxBufferSize = ServerCapabilities.MaxContentLength;
        options.ApplicationMaxBufferSize = ServerCapabilities.MaxContentLength;
      });
    }
  }
}
