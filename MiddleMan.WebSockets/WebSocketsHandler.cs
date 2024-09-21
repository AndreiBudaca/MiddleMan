using MiddleMan.WebSockets.Model;
using System.Net.WebSockets;
using System.Text;

namespace MiddleMan.WebSockets
{
    public class WebSocketsHandler : IWebSocketsHandler
    {
        private readonly Dictionary<int, WebSocketData> websockets = new Dictionary<int, WebSocketData>();

        public void Accept(WebSocketData data)
        {
            websockets.Add(data.Id, data);
        }

        public bool Close(int webSocketId)
        {
            var success = websockets.TryGetValue(webSocketId, out var webSocket);
            if (!success) return false;

            webSocket?.SocketFinished.SetResult(true);
            return true;
        }

        public async Task<string> CommunicateAsync(int webSocketId, string message, CancellationToken cancellationToken)
        {
            var success = websockets.TryGetValue(webSocketId, out var webSocket);
            if (!success || webSocket == null) return string.Empty;

            await webSocket.Socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)), WebSocketMessageType.Text, true, cancellationToken);

            var responseBuffer = new ArraySegment<byte>(new byte[1024]);
            await webSocket.Socket.ReceiveAsync(responseBuffer, cancellationToken);

            return Encoding.UTF8.GetString(responseBuffer.TakeWhile(b => b != 0).ToArray());
        }
    }
}
