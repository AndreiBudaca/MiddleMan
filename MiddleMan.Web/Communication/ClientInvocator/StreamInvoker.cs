using Microsoft.AspNetCore.SignalR;
using MiddleMan.Communication;
using MiddleMan.Communication.Adapters;
using MiddleMan.Service.WebSocketClientConnections.Classes;
using MiddleMan.Web.Communication.Adapters;
using MiddleMan.Web.Communication.Metadata;
using MiddleMan.Web.Controllers.ActionResults;
using MiddleMan.Web.Infrastructure.Identity;

namespace MiddleMan.Web.Communication.ClientInvocator
{
  public class StreamInvoker(IntraServerCommunicationManager intraServerCommunicationManager,
    StreamingCommunicationManager streamingCommunicationManager,
    ILogger logger) : IClientInvoker
  {
    private readonly IntraServerCommunicationManager intraServerCommunicationManager = intraServerCommunicationManager;
    private readonly StreamingCommunicationManager streamingCommunicationManager = streamingCommunicationManager;
    private readonly ILogger logger = logger;
    private readonly Guid correlation = Guid.NewGuid();

    public Task Cleanup()
    {
      return intraServerCommunicationManager.ClearRequestSession(correlation);
    }

    public async Task<IControllerResult> Invoke(HttpContext httpContext, string method, ClientConnection webSocketClientConnection,
     ISingleClientProxy hubClient, CancellationToken cancellationToken)
    {
      var adapter = new HttpRequestAdapter(httpContext.Request, new HttpUser
      {
        Identifier = httpContext.User.Identifier(),
      }, webSocketClientConnection.ClientCapabilities.SendHTTPMetadata);

      logger.LogInformation("Starting stream invocation. Correlation ID: {CorrelationId}, Method: {Method}, IsSameServerConnection: {IsSameServerConnection}", correlation, method, webSocketClientConnection.SameServerConnection);
      try
      {
        await intraServerCommunicationManager.RegisterRequestSession(correlation, webSocketClientConnection.SameServerConnection);
        await hubClient.SendAsync(method, correlation, cancellationToken);

        return webSocketClientConnection.SameServerConnection ?
          await SameServerStreamInvocation(correlation, adapter, cancellationToken) :
          await IntraServerStreamInvocation(correlation, adapter, cancellationToken);
      }
      catch (Exception ex)
      {
        logger.LogError("Error during streaming invocation: {message}", ex.Message);
        return new StatusResult(StatusCodes.Status504GatewayTimeout);
      }
    }

    private async Task<IControllerResult> SameServerStreamInvocation(Guid correlation, IDataWriterAdapter adapter, CancellationToken cancellationToken)
    {
      await streamingCommunicationManager.WriteAsync(adapter, correlation, cancellationToken);

      return new MiddleManClientStreamingResult(streamingCommunicationManager.ReadAsync(correlation, cancellationToken), cancellationToken);
    }

    private async Task<IControllerResult> IntraServerStreamInvocation(Guid correlation, IDataWriterAdapter adapter, CancellationToken cancellationToken)
    {
      await intraServerCommunicationManager.WriteRequestAsync(adapter, correlation, cancellationToken);

      return new MiddleManClientStreamingResult(intraServerCommunicationManager.ReadResponseAsync(correlation, cancellationToken), cancellationToken);
    }
  }
}