using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiddleMan.Web.Infrastructure.Attributes;
using MiddleMan.WebSockets;
using MiddleMan.WebSockets.Model;

namespace MiddleMan.Web.Controllers.WebSockets
{
  [Authorize]
  [Route("[controller]")]
  public class WebSocketsController(IWebSocketsHandler webSocketsHandler) : Controller
  {
    private readonly IWebSocketsHandler webSocketsHandler = webSocketsHandler;

    [AllowAnonymous]
    [ClientToken]
    [Route("ws")]
    public async Task Subscribe()
    {
      if (!HttpContext.WebSockets.IsWebSocketRequest)
      {
        HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
      }

      using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
      if (webSocket == null)
      {
        HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
      }

      var socketFinished = new TaskCompletionSource<object>();

      webSocketsHandler.Accept(new WebSocketData
      {
        Id = 0,
        Socket = webSocket,
        SocketFinished = socketFinished
      });

      await socketFinished.Task;
    }

    [HttpPost]
    [Route("{websocketId}")]
    public async Task<IActionResult> Send([FromRoute] int websocketId, [FromBody] string data, CancellationToken cancellationToken)
    {
      var response = await webSocketsHandler.CommunicateAsync(websocketId, data, cancellationToken);

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
