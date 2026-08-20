using MiddleMan.Communication;
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
      var responseData = communicationManager.ReadResponseAsync(correlation, cancellationToken);

      var (requestMetadata, responseDataStream) = await LenghtPrefixConverter
          .ReadFromChunkedData<HttpRequestMetadata>(responseData, cancellationToken);

      return new RequestData
      {
        Metadata = requestMetadata,
        Data = responseDataStream
      };
    }
  }
}