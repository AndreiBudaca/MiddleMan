using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using MiddleMan.Communication;
using MiddleMan.Communication.Adapters;
using MiddleMan.Core.Extensions;
using MiddleMan.Service.WebSocketClientConnections.Classes;
using MiddleMan.Web.Communication.Adapters;
using MiddleMan.Web.Communication.Metadata;

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

    public async Task<(HttpResponseMetadata?, IAsyncEnumerable<byte[]>?)> Invoke(IAsyncEnumerable<byte[]> data, HttpRequestMetadata metadata, string method, ClientConnection webSocketClientConnection,
     ISingleClientProxy hubClient, CancellationToken cancellationToken)
    {
      var adapter = new MetadataAdaptorDecorator(data, metadata, webSocketClientConnection.ClientCapabilities.SendHTTPMetadata);

      logger.LogInformation("Starting stream invocation. Correlation ID: {CorrelationId}, Method: {Method}, IsSameServerConnection: {IsSameServerConnection}", correlation, method, webSocketClientConnection.SameServerConnection);
      await intraServerCommunicationManager.RegisterRequestSession(correlation, webSocketClientConnection.SameServerConnection);
      await hubClient.SendAsync(method, correlation, cancellationToken);

      var responseData = webSocketClientConnection.SameServerConnection ?
        await SameServerStreamInvocation(correlation, adapter, cancellationToken) :
        await IntraServerStreamInvocation(correlation, adapter, cancellationToken);

      return await ReadMetadata(responseData, cancellationToken);
    }

    private async Task<IAsyncEnumerable<byte[]>> SameServerStreamInvocation(Guid correlation, IDataWriterAdaptor adapter, CancellationToken cancellationToken)
    {
      await streamingCommunicationManager.WriteAsync(adapter, correlation, cancellationToken);
      return streamingCommunicationManager.ReadAsync(correlation, cancellationToken);
    }

    private async Task<IAsyncEnumerable<byte[]>> IntraServerStreamInvocation(Guid correlation, IDataWriterAdaptor adapter, CancellationToken cancellationToken)
    {
      await intraServerCommunicationManager.WriteRequestAsync(adapter, correlation, cancellationToken);
      return intraServerCommunicationManager.ReadResponseAsync(correlation, cancellationToken);
    }

    private async Task<(HttpResponseMetadata?, IAsyncEnumerable<byte[]>?)> ReadMetadata(IAsyncEnumerable<byte[]> data, CancellationToken cancellationToken)
    {
      HttpResponseMetadata? metadata = null;

      var metadataLengthBytes = await data.EnumerateUntil(4, 0, cancellationToken);
      data = metadataLengthBytes.Next;

      var metadataLength = BitConverter.ToInt32(metadataLengthBytes.Received, 0);

      if (metadataLength > 0)
      {
        var metadataBytes = await data.EnumerateUntil(metadataLength, 0, cancellationToken);
        data = metadataBytes.Next;

        var metadataJson = Encoding.UTF8.GetString(metadataBytes.Received, 0, metadataLength);
        metadata = JsonSerializer.Deserialize<HttpResponseMetadata>(metadataJson);
      }

      return (metadata, data);
    }
  }
}