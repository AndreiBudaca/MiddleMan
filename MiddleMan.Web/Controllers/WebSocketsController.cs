using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiddleMan.WebSockets;
using MiddleMan.WebSockets.Model;

namespace MiddleMan.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class WebSocketsController(WebSocketsHandler webSocketsHandler) : ControllerBase
    {
        private readonly WebSocketsHandler webSocketsHandler = webSocketsHandler;

        [Route("/ws")]
        public async Task Subscribe()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                var socketFinished = new TaskCompletionSource<object>();

                webSocketsHandler.Accept(new WebSocketData
                {
                    Id = 0,
                    Socket = webSocket,
                    SocketFinished = socketFinished
                });

                await socketFinished.Task;
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }

        [HttpPost]
        [Route("{websocketId}")]
        public async Task<IActionResult> Send([FromRoute] int websocketId, [FromBody] string data)
        {
            var response = await webSocketsHandler.Communicate(websocketId, data);

            if (string.IsNullOrEmpty(response)) return NotFound();
            return Ok(response);
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok("merge hehe");
        }
    }
}
