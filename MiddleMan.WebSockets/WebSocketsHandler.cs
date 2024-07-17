using MiddleMan.WebSockets.Model;
using System.Net.WebSockets;
using System.Text;

namespace MiddleMan.WebSockets
{
    public class WebSocketsHandler
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

            webSocket.SocketFinished.SetResult(true);
            return true;
        }

        public async Task<string> Communicate(int webSocketId, string message)
        {
            var success = websockets.TryGetValue(webSocketId, out var webSocket);
            if (!success) return string.Empty;

            await webSocket.Socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)), WebSocketMessageType.Text, true, CancellationToken.None);

            var responseBuffer = new ArraySegment<byte>(new byte[1024]);
            await webSocket.Socket.ReceiveAsync(responseBuffer, CancellationToken.None);

            return Encoding.UTF8.GetString(responseBuffer.TakeWhile(b => b != 0).ToArray());
        }
    }
}
