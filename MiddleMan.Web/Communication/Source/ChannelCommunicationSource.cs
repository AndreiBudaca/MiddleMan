using System.Runtime.CompilerServices;
using MiddleMan.Communication;
using MiddleMan.Core;
using MiddleMan.Core.DataChunkConverters;
using MiddleMan.Web.Communication.Metadata;

namespace MiddleMan.Web.Communication.Source
{
  public class ChannelCommunicationSource(
    Guid correlation,
    IntraServerCommunicationManager communicationManager
  ) : ICommunicationSource
  {
    public async Task<RequestData> ReadData(CancellationToken cancellationToken = default)
    {
      HttpRequestMetadata? requestMetadata;
      IAsyncEnumerable<DataChunk>? requestDataStream;

      try
      {
        var requestData = communicationManager.ReadRequestAsync(correlation, cancellationToken);

        (requestMetadata, requestDataStream) = await LenghtPrefixConverter
            .ReadFromChunkedData<HttpRequestMetadata>(requestData, cancellationToken);
      }
      catch
      {
        await communicationManager.MarkRequestHandled(correlation);
        throw;
      }

      return new RequestData
      {
        Metadata = requestMetadata,
        Data = ConsumeStream(requestDataStream, cancellationToken),
      };
    }

    private async IAsyncEnumerable<DataChunk> ConsumeStream(IAsyncEnumerable<DataChunk>? stream, [EnumeratorCancellation] CancellationToken cancellationToken)
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
        await communicationManager.MarkRequestHandled(correlation);
      }
    }
  }
}