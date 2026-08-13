namespace MiddleMan.Web.Communication.Source
{
  public interface ICommunicationSource
  {
    public Task<RequestData> ReadData(CancellationToken cancellationToken = default);
  }
}