using MiddleMan.Core.Extensions;

namespace MiddleMan.Core.DataChunkConverters
{
  public static class LenghtPrefixConverter
  {
    public static DataChunk ToDataChunk<T>(T? value)
    {
      if (value == null) return EmptyDataChunk();

      var metadataJson = System.Text.Json.JsonSerializer.Serialize(value);
      var metadataBytes = System.Text.Encoding.UTF8.GetBytes(metadataJson);
      var lengthBytes = BitConverter.GetBytes(metadataBytes.Length);

      return new DataChunk
      {
        Data = [.. lengthBytes, .. metadataBytes],
        Length = lengthBytes.Length + metadataBytes.Length
      };
    }

    public async static Task<(T?, IAsyncEnumerable<DataChunk>)> ReadFromChunkedData<T>(
      IAsyncEnumerable<DataChunk> dataStream, 
      CancellationToken cancellationToken = default
    ) where T : class
    {
      var lengthResult = await dataStream.EnumerateUntil(4, cancellationToken);
      dataStream = lengthResult.Next;

      var length = BitConverter.ToInt32(lengthResult.Received.Data);

      if (length == 0) return (null, dataStream);

      var metadataResult = await dataStream.EnumerateUntil(length, cancellationToken);
      dataStream = metadataResult.Next;

      var metadata = System.Text.Json.JsonSerializer.Deserialize<T>(metadataResult.Received.Data);
      return (metadata, dataStream);
    }

    private static DataChunk EmptyDataChunk()
    {
      var lengthBytes = BitConverter.GetBytes(0);
      return new DataChunk
      {
        Data = lengthBytes,
        Length = lengthBytes.Length
      };
    }
  }
}