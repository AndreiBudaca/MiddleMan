using MiddleMan.Web.Communication.Target;

namespace MiddleMan.Web.Communication.Resolver
{
  public class ManualTargetResolver(ICommunicationTarget? target) : ICommunicationTargetResolver
  {
    public Task<ICommunicationTarget?> ResolveTarget(TargetDescription targetDescription, CancellationToken cancellationToken = default)
    {
      return Task.FromResult(target);
    }
  }
}