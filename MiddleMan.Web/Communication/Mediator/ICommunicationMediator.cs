namespace MiddleMan.Web.Communication.Mediator
{
  public interface ICommunicationMediator
  {
    public Task<bool> Mediate(CancellationToken cancellationToken = default);
  }
}