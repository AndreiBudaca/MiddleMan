using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MiddleMan.Data.InMemory;
using MiddleMan.Service.WebSocketClients;
using MiddleMan.Web.Communication;
using MiddleMan.Web.Communication.Adapters;
using MiddleMan.Web.Hubs;
using MiddleMan.Web.Infrastructure.Identity;

namespace MiddleMan.Web.Controllers.WebSockets
{
  [Authorize]
  [Route("api/websockets")]
  public class WebSocketsController(
    IHubContext<PlaygroundHub> hubContext,
    IWebSocketClientsService webSocketClientsService,
    IInMemoryContext inMemoryContext,
    CommunicationManager communicationManager
    ) : Controller
  {
    private readonly IInMemoryContext inMemoryContext = inMemoryContext;
    private readonly IHubContext<PlaygroundHub> hubContext = hubContext;
    private readonly IWebSocketClientsService webSocketClientsService = webSocketClientsService;
    private readonly CommunicationManager communicationManager = communicationManager;

    [HttpPost]
    [RequestSizeLimit(1_000_000_000)]
    [Route("{webSocketClientName}/{method}")]
    public async Task<IActionResult> Send([FromRoute] string webSocketClientName, [FromRoute] string method, CancellationToken cancellationToken)
    {
      if (string.IsNullOrWhiteSpace(webSocketClientName)) return base.NotFound();
      if (string.IsNullOrWhiteSpace(method)) return NotFound();

      var webSocketClient = await webSocketClientsService.GetWebSocketClient(User.Identifier(), webSocketClientName);
      if (webSocketClient == null) return NotFound();

      if (string.IsNullOrWhiteSpace(webSocketClient.ConnectionId)) return base.NotFound();

      var hubClient = hubContext.Clients.Client(webSocketClient.ConnectionId);
      if (hubClient == null)
      {
        await webSocketClientsService.DeleteWebSocketClientConnection(User.Identifier(), webSocketClientName);
        return NotFound();
      }

      var correlation = Guid.NewGuid();

      await hubClient.SendAsync(method, correlation, cancellationToken);

      Task? writeTask = null;
      try
      {
        _ = communicationManager.WriteAsync(new StreamToWriterAdapter(Request.Body), correlation);
        var response = communicationManager.ReadAsync(correlation);

        return new BinaryResult(response, cancellationToken);
      }
      finally
      {
        if (writeTask != null) await writeTask;
      }
    }
  }

  public class BinaryResult : ActionResult
  {
    private readonly IAsyncEnumerable<byte[]>? response = null;
    private readonly CancellationToken? cancellationToken = null;

    public BinaryResult() { }

    public BinaryResult(IAsyncEnumerable<byte[]> response, CancellationToken cancellationToken)
    {
      this.response = response;
      this.cancellationToken = cancellationToken;
    }

    public override async Task ExecuteResultAsync(ActionContext context)
    {
      context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
      context.HttpContext.Response.Headers.ContentType = "application/octet-stream";
      if (response == null) return;

      await foreach (var chunk in response)
      {
        await context.HttpContext.Response.BodyWriter.WriteAsync(chunk, cancellationToken ?? CancellationToken.None);
      }

      await context.HttpContext.Response.BodyWriter.CompleteAsync();
    }
  }
}
