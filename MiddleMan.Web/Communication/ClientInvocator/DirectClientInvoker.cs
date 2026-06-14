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
      if (!ValidateLength(metadata))
      {
        return (new HttpResponseMetadata(StatusCodes.Status413PayloadTooLarge), null);
      }

      if (ServerCapabilities.VerboseLogging)
      {
        logger.LogInformation("Starting direct invocation. Method: {Method}, IsSameServerConnection: {IsSameServerConnection}", method, webSocketClientConnection.SameServerConnection);
      }
     
      var communicationManager = new DirectInvocationCommunicationManager(data, metadata, webSocketClientConnection.ClientCapabilities.SendHTTPMetadata);
      var response = await communicationManager.InvokeAsync(hubClient, method, cancellationToken);
      
      return (response.Metadata, response.Data.AsAsyncEnumerable());
    }

    public async Task Send(IAsyncEnumerable<byte[]> data, HttpRequestMetadata metadata, string method, ClientConnection webSocketClientConnection, ISingleClientProxy hubClient, CancellationToken cancellationToken)
    {
      if (!ValidateLength(metadata))
      {
        logger.LogError("Dropping invocation due to payload too large. Method: {Method}, ContentLength: {ContentLength}", method, metadata.Headers.TryGetValue(HeaderNames.ContentLength, out var contentLengthValue) ? contentLengthValue : "unknown");
        return;
      }

      if (ServerCapabilities.VerboseLogging)
      {
        logger.LogInformation("Starting direct invocation. Method: {Method}, IsSameServerConnection: {IsSameServerConnection}", method, webSocketClientConnection.SameServerConnection);
      }
     
      var communicationManager = new DirectInvocationCommunicationManager(data, metadata, webSocketClientConnection.ClientCapabilities.SendHTTPMetadata);
      await communicationManager.SendAsync(hubClient, method, cancellationToken);
    }

    private static bool ValidateLength(HttpRequestMetadata metadata)
    {
      if (!metadata.Headers.TryGetValue(HeaderNames.ContentLength, out var contentLengthValue))
      {
        return false;
      }

      if (!int.TryParse(contentLengthValue, out int contentLength))
      {
        return false;
      }

      return contentLength <= ServerCapabilities.MaxChunkSize;
    }
  }
}