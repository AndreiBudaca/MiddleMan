using MiddleMan.Core;
using MiddleMan.Web.Communication.Mediator.Classes;
using MiddleMan.Web.Communication.Resolver;

namespace MiddleMan.Web.Communication.Mediator
{
  public class SimpleMediator(
    ILogger logger,
    CommunicationSourceContext context,
    ICommunicationTargetResolver targetResolver,
    TargetDescription targetDescription)
    : ICommunicationMediator
  {
    private readonly ILogger logger = logger;
    private readonly CommunicationSourceContext context = context;
    private readonly ICommunicationTargetResolver targetResolver = targetResolver;
    private readonly TargetDescription targetDescription = targetDescription;

    public virtual Task<bool> Mediate(CancellationToken cancellationToken = default)
    {
      return Mediate(context, cancellationToken);
    }

    protected async Task<bool> Mediate(CommunicationSourceContext context, CancellationToken cancellationToken)
    {
            var target = await targetResolver.ResolveTarget(targetDescription, cancellationToken);
      if (target == null) return false;

      LogInvocation();

      var requestData = await context.Source.ReadData(cancellationToken);

      var responseSource = await target.WriteData(requestData, cancellationToken);
      if (context.Sink == null) return true;

      if (responseSource == null)
      {
        await context.Sink.WriteData(null, cancellationToken);
        return true;
      }

      await using var bufferedResponseSource = new BufferedResponseData(responseSource);
      var responseData = await bufferedResponseSource.Read(cancellationToken);
      await context.Sink.WriteData(responseData, cancellationToken);

      return true;
    }

    private void LogInvocation()
    {
      if (!ServerCapabilities.VerboseLogging) return;
      if (!logger.IsEnabled(LogLevel.Information)) return;

      logger.LogInformation("Invoking {WebSocketClientName}, method: {Method}",
        targetDescription.Name,
        targetDescription.Method);
    }
  }
}