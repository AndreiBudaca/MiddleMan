using MiddleMan.WebSockets;

namespace MiddleMan.Web.Infrastructure.Configuration
{
    public static class WebSocketConfig
    {
        public static void AddWebSocketHandler(this IServiceCollection services)
        {
            services.AddSingleton<IWebSocketsHandler, WebSocketsHandler>();
        }
    }
}
