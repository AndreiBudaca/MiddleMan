using System.Net.WebSockets;

namespace MiddleMan.WebSockets.Model
{
    public class WebSocketData
    {
        public int Id { get; set; }

        public WebSocket Socket { get; set; }

        public TaskCompletionSource<object> SocketFinished { get; set; }
    }
}
