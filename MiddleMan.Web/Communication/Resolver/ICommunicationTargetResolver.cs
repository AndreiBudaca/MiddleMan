using MiddleMan.Web.Communication.Target;

namespace MiddleMan.Web.Communication.Resolver
{
  public interface ICommunicationTargetResolver
  {
    public Task<ICommunicationTarget?> ResolveTarget(TargetDescription targetDescription, CancellationToken cancellationToken = default);
  }
}