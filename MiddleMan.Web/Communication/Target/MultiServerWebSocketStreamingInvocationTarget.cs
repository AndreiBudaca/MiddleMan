using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.SignalR;
using MiddleMan.Communication;
using MiddleMan.Core;
using MiddleMan.Core.Conversions;
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
      
      // Send the correlation ID and metadata to the client to initiate the streaming session
      await clientProxy.SendAsync(method, correlation, data?.Metadata, cancellationToken);

      await WriteData(correlation, data, cancellationToken);

      var responseData = manager.ReadResponseAsync(correlation, cancellationToken);

      // Extract metadata from the response data
      var (responseMetadata, responseDataStream) = await LenghtPrefixConverter
        .ReadFromChunkedData<HttpResponseMetadata>(responseData, cancellationToken);

      // Return the rest of the response data as an IAsyncEnumerable<DataChunk>
      return new ResponseData
      {
        Metadata = responseMetadata,
        Data = ConsumeStream(correlation, responseDataStream, cancellationToken)
      };
    }

    private async Task WriteData(Guid correlation, RequestData? data, CancellationToken cancellationToken = default)
    {
      try
      {
        var metadataChunk = LenghtPrefixConverter.ToDataChunk(data?.Metadata);
        var requestData = data?.Data ?? new EmptyAsyncEnumerable<DataChunk>();
        
        await manager.WriteRequestAsync(requestData.PrependItems(metadataChunk, cancellationToken), correlation, cancellationToken);
        await manager.ClearRequestSession(correlation, false);
      }
      catch
      {
        await manager.ClearRequestSession(correlation, true);
        throw;
      }
    }

    public async Task<ResponseData> ReadData(Guid correlation, CancellationToken cancellationToken = default)
    {
      HttpResponseMetadata? responseMetadata;
      IAsyncEnumerable<DataChunk>? requestDataStream;

      try
      {
        var responseData = manager.ReadResponseAsync(correlation, cancellationToken);

        (responseMetadata, requestDataStream) = await LenghtPrefixConverter
            .ReadFromChunkedData<HttpResponseMetadata>(responseData, cancellationToken);
      }
      catch
      {
        await manager.MarkResponseHandled(correlation);
        throw;
      }

      return new ResponseData
      {
        Metadata = responseMetadata,
        Data = ConsumeStream(correlation, requestDataStream, cancellationToken),
      };
    }

    private async IAsyncEnumerable<DataChunk> ConsumeStream(Guid correlation, IAsyncEnumerable<DataChunk>? stream, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
      try
      {
        if (stream == null) yield break;

        await foreach (var item in stream.WithCancellation(cancellationToken))
        {
          yield return item;
        }
      }
      finally
      {
        await manager.MarkResponseHandled(correlation);
      }
    }
  }
}