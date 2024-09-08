using MiddleMan.WebSockets;

namespace MiddleMan.Web.Infrastructure
{
    public static class WebSocketConfig
    {
        public static void AddWebSocketHandler(this IServiceCollection services)
        {
            services.AddSingleton<IWebSocketsHandler, WebSocketsHandler>();
        }
    }
}
