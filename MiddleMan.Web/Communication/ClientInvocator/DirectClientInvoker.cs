using Microsoft.AspNetCore.SignalR;
using MiddleMan.Core;
using MiddleMan.Service.WebSocketClientConnections.Classes;
using MiddleMan.Web.Communication.Metadata;
using MiddleMan.Web.Controllers.ActionResults;
using MiddleMan.Web.Infrastructure.Identity;

namespace MiddleMan.Web.Communication.ClientInvocator
{
  public class DirectClientInvoker(ILogger logger) : IClientInvoker
  {
    private readonly ILogger logger = logger;

    public Task Cleanup()
    {
      return Task.CompletedTask;
    }

    public async Task<IControllerResult> Invoke(HttpContext httpContext, string method, ClientConnection webSocketClientConnection,
     ISingleClientProxy hubClient, CancellationToken cancellationToken)
    {
      if (httpContext.Request.ContentLength > ServerCapabilities.MaxChunkSize)
      {
        return new StatusResult(StatusCodes.Status413PayloadTooLarge);
      }

      logger.LogInformation("Starting direct invocation. Method: {Method}, IsSameServerConnection: {IsSameServerConnection}", method, webSocketClientConnection.SameServerConnection);
      try
      {
        var communicationManager = new DirectInvocationCommunicationManager(httpContext.Request, new HttpUser
        {
          Identifier = httpContext.User.Identifier(),
        }, sendMetadata: webSocketClientConnection.ClientCapabilities.SendHTTPMetadata);

        var response = await communicationManager.InvokeAsync(hubClient, method, cancellationToken);
        return new MiddleManClientDirectInvocationResult(response, cancellationToken);
      }
      catch (Exception ex)
      {
        logger.LogError("Error during direct invocation: {message}", ex.Message);
        return new StatusResult(StatusCodes.Status504GatewayTimeout);
      }
    }
  }
}