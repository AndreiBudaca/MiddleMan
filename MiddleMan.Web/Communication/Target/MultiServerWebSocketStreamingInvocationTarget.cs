using Microsoft.AspNetCore.SignalR;
using MiddleMan.Communication;
using MiddleMan.Core;
using MiddleMan.Core.DataChunkConverters;
using MiddleMan.Core.Extensions;
using MiddleMan.Core.Extensions.Classes.AsyncEnumerable;
using MiddleMan.Web.Communication.Metadata;

namespace MiddleMan.Web.Communication.Target
{
  public class MultiServerWebSocketStreamingInvocationTarget(
    IntraServerCommunicationManager manager,
    ISingleClientProxy clientProxy,
    string method
  ) : ICommunicationTarget
  {
    private readonly IntraServerCommunicationManager manager = manager;
    private readonly ISingleClientProxy clientProxy = clientProxy;
    private readonly string method = method;

    public async Task<ResponseData?> WriteData(RequestData? data, CancellationToken cancellationToken = default)
    {
      var correlation = Guid.NewGuid();

      try
      {
        // Create request session
        await manager.RegisterRequestSession(correlation);

        // Send the correlation ID and metadata to the client to initiate the streaming session
        await clientProxy.SendAsync(method, correlation, data?.Metadata, cancellationToken);

        var metadataChunk = LenghtPrefixConverter.ToDataChunk(data?.Metadata);
        var requestData = data?.Data ?? new EmptyAsyncEnumerable<DataChunk>();
        await manager.WriteRequestAsync(requestData.PrependItems(metadataChunk, cancellationToken), correlation, cancellationToken);

        var responseData = manager.ReadResponseAsync(correlation, cancellationToken);

        // Extract metadata from the response data
        var (responseMetadata, responseDataStream) = await LenghtPrefixConverter
          .ReadFromChunkedData<HttpResponseMetadata>(responseData, cancellationToken);

        // Return the rest of the response data as an IAsyncEnumerable<DataChunk>
        return new ResponseData
        {
          Metadata = responseMetadata,
          Data = responseDataStream
        };
      }
      finally
      {
        await manager.ClearRequestSession(correlation);
      }
    }
  }
}