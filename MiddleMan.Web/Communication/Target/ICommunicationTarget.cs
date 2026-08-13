namespace MiddleMan.Web.Communication.Target
{
  public interface ICommunicationTarget
  {
    public Task<ResponseData?> WriteData(RequestData? data, CancellationToken cancellationToken = default);
  }
}