using Microsoft.AspNetCore.SignalR;
using Microsoft.Net.Http.Headers;
using MiddleMan.Core;
using MiddleMan.Core.Extensions;
using MiddleMan.Service.WebSocketClientConnections.Classes;
using MiddleMan.Web.Communication.Metadata;

namespace MiddleMan.Web.Communication.ClientInvocator
{
  public class DirectClientInvoker(ILogger logger) : IClientInvoker
  {
    private readonly ILogger logger = logger;

    public Task Cleanup()
    {
      return Task.CompletedTask;
    }

    public async Task<(HttpResponseMetadata?, IAsyncEnumerable<byte[]>?)> Invoke(IAsyncEnumerable<byte[]> data, HttpRequestMetadata metadata, string method, ClientConnection webSocketClientConnection,
     ISingleClientProxy hubClient, CancellationToken cancellationToken)
    {
      if (!int.TryParse(metadata.Headers[HeaderNames.ContentLength], out int contentLength) || contentLength > ServerCapabilities.MaxChunkSize)
      {
        return (new HttpResponseMetadata(StatusCodes.Status413PayloadTooLarge), null);
      }

      logger.LogInformation("Starting direct invocation. Method: {Method}, IsSameServerConnection: {IsSameServerConnection}", method, webSocketClientConnection.SameServerConnection);
     
      var communicationManager = new DirectInvocationCommunicationManager(data, metadata, webSocketClientConnection.ClientCapabilities.SendHTTPMetadata);
      var response = await communicationManager.InvokeAsync(hubClient, method, cancellationToken);
      
      return (response.Metadata, response.Data.AsAsyncEnumerable());
    }
  }
}