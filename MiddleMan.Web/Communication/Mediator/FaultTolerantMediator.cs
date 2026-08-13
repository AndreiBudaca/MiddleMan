using MiddleMan.Core;
using MiddleMan.Web.Communication.Mediator.Classes;
using MiddleMan.Web.Communication.Resolver;
using MiddleMan.Web.Communication.Source;

namespace MiddleMan.Web.Communication.Mediator
{
  public class FaultTolerantMediator(
    ILogger<ICommunicationMediator> logger,
    CommunicationSourceContext context,
    ICommunicationTargetResolver targetResolver,
    TargetDescription targetDescription)
    : ICommunicationMediator
  {
    private readonly ILogger<ICommunicationMediator> logger = logger;
    private readonly CommunicationSourceContext context = context;
    private readonly ICommunicationTargetResolver targetResolver = targetResolver;
    private readonly TargetDescription targetDescription = targetDescription;

    public async Task<bool> Mediate(CancellationToken cancellationToken = default)
    {
      await using var bufferedRequestSource = new BufferedCommunicationSource(context.Source);

      var retryCount = 1;
      var wasSuccessfulInvocation = false;

      using var communicationFailedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

      do
      {
        try
        {
          communicationFailedCts.TryReset();

          var target = await targetResolver.ResolveTarget(targetDescription, communicationFailedCts.Token);
          if (target == null) return false;

          LogInvocation(retryCount);

          var requestData = await bufferedRequestSource.ReadData(communicationFailedCts.Token);

          var responseSource = await target.WriteData(requestData, communicationFailedCts.Token);
          if (context.Sink == null) return true;

          if (responseSource == null)
          {
            await context.Sink.WriteData(null, communicationFailedCts.Token);
            return true;
          }

          await using var bufferedResponseSource = new BufferedResponseData(responseSource);
          await context.Sink.WriteData(await bufferedResponseSource.Read(communicationFailedCts.Token), communicationFailedCts.Token);
          return true;
        }
        catch (Exception ex)
        {
          LogInvocationError(ex);
          communicationFailedCts.Cancel();
        }
      } while (!wasSuccessfulInvocation && ServerCapabilities.FaultToleranceEnabled && retryCount++ < ServerCapabilities.MaxRetryAttempts);

      return wasSuccessfulInvocation;
    }

    private void LogInvocation(int retryCount)
    {
      if (!ServerCapabilities.VerboseLogging) return;

      if (retryCount == 1)
      {
        logger.LogInformation("Invoking {WebSocketClientName}, method: {Method}",
          targetDescription.Name,
          targetDescription.Method);

        return;
      }

      logger.LogWarning("Retrying invocation for {WebSocketClientName}, method: {Method}. Attempt {RetryCount} of {MaxRetryAttempts}",
        targetDescription.Name,
        targetDescription.Method,
        retryCount,
        ServerCapabilities.MaxRetryAttempts);
    }

    private void LogInvocationError(Exception ex)
    {
      logger.LogError(ex, "Invocation error for {WebSocketClientName}, method: {Method}. Error: {ErrorMessage}",
        targetDescription.Name,
        targetDescription.Method,
        ex.Message);
    }
  }
}