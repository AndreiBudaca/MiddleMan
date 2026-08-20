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

    public DataChunk(int length)
    {
      // TO DO: Use array pool to avoid memory fragmentation and GC overhead
      Data = new byte[length];
      Length = length;
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

    public void Append(byte[] data, int length)
    {
      if (Length + length > Data.Length)
      {
        throw new InvalidOperationException("DataChunk is full.");
      }

      Array.Copy(data, 0, Data, Length, length);
      Length += length;
    }
  }
}