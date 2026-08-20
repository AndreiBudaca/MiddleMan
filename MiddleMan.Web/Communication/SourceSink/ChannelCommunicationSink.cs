using MiddleMan.Communication;
using MiddleMan.Core;
using MiddleMan.Core.DataChunkConverters;
using MiddleMan.Core.Extensions;
using MiddleMan.Core.Extensions.Classes.AsyncEnumerable;
using MiddleMan.Web.Communication.Metadata;

namespace MiddleMan.Web.Communication.SourceSink
{
  public class ChannelCommunicationSink(
    Guid correlation,
    IntraServerCommunicationManager communicationManager
  ) : ICommunicationSourceSink
  {
    public async Task WriteData(ResponseData? data, CancellationToken cancellationToken = default)
    {
      var metadataChunk = LenghtPrefixConverter.ToDataChunk(data?.Metadata);
      var dataStream = data?.Data ?? new EmptyAsyncEnumerable<DataChunk>();

      await communicationManager.WriteRequestAsync(dataStream.PrependItems(metadataChunk, cancellationToken), correlation, cancellationToken);
    }
  }
}