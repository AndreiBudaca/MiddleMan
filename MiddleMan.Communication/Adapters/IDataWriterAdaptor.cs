namespace MiddleMan.Communication.Adapters
{
  public interface IDataWriterAdaptor
  {
    public IAsyncEnumerable<byte[]> Adapt(); 
  }
}
