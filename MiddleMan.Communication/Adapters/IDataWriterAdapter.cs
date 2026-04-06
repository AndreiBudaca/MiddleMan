namespace MiddleMan.Communication.Adapters
{
  public interface IDataWriterAdapter
  {
    public IAsyncEnumerable<byte[]> Adapt(); 
  }
}
