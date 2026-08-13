namespace MiddleMan.Web.Communication.SourceSink
{
  public interface ICommunicationSourceSink
  {
    public Task WriteData(ResponseData? data, CancellationToken cancellationToken = default);
  }
}