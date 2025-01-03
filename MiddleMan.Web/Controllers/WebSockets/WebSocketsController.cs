using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace MiddleMan.Web.Controllers.WebSockets
{
  [Authorize]
  [Route("[controller]")]
  public class WebSocketsController(IHubContext hubContext) : Controller
  {
    private readonly IHubContext hubContext = hubContext;

    [HttpPost]
    [Route("{websocketId}")]
    public async Task<IActionResult> Send([FromRoute] int websocketId, [FromBody] string data, CancellationToken cancellationToken)
    {
      await hubContext.Clients.All.SendAsync("ReceiveMessage", data, cancellationToken);

      return Ok("worked");
    }

    [HttpGet]
    public IActionResult GetAll()
    {
      return Ok("merge hehe");
    }
  }
}
