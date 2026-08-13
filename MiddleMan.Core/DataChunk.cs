namespace MiddleMan.Core
{
  public class DataChunk
  {
    public byte[] Data { get; set; }
    public int Length { get; set; }

    public DataChunk()
    {
      Data = new byte[ServerCapabilities.MaxChunkSize];
    }

    public DataChunk(byte[] data)
    {
      Data = data;
      Length = data.Length;
    }

    public DataChunk(byte[] data, int length)
    {
      Data = data;
      Length = length;
    }
  }
}