using MiddleMan.Communication;
using MiddleMan.Core;
using MiddleMan.Core.Conversions;
using MiddleMan.Core.Extensions;
using MiddleMan.Core.Extensions.Classes.AsyncEnumerable;


namespace MiddleMan.Web.Communication.SourceSink
{
  public class ChannelCommunicationSink(
    Guid correlation,
    IntraServerCommunicationManager communicationManager
  ) : ICommunicationSourceSink
  {
    public async Task WriteData(ResponseData? data, CancellationToken cancellationToken = default)
    {
      try
      {
        var metadataChunk = LenghtPrefixConverter.ToDataChunk(data?.Metadata);
        var dataStream = data?.Data ?? new EmptyAsyncEnumerable<DataChunk>();

        await communicationManager.WriteResponseAsync(dataStream.PrependItems(metadataChunk, cancellationToken), correlation, cancellationToken);
        await communicationManager.ClearResponseSession(correlation, false);
      }
      catch
      {
        await communicationManager.ClearResponseSession(correlation, true);
        throw;
      }
    }
  }
}