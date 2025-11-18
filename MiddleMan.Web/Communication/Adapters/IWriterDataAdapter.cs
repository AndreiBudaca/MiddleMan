namespace MiddleMan.Web.Communication.Adapters
{
  public interface IDataWriterAdapter
  {
    public IAsyncEnumerable<byte[]> Adapt(); 
  }
}
