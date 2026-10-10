using MiddleMan.Core;
using MiddleMan.Web.Communication.Resolver;
using MiddleMan.Web.Communication.Source;

namespace MiddleMan.Web.Communication.Mediator
{
  public class FaultTolerantMediator(
    ILogger logger,
    CommunicationSourceContext context,
    ICommunicationTargetResolver targetResolver,
    TargetDescription targetDescription)
    : SimpleMediator(logger, context, targetResolver, targetDescription)
  {
    private readonly ILogger logger = logger;
    private readonly CommunicationSourceContext context = context;
    private readonly TargetDescription targetDescription = targetDescription;

    public override async Task<bool> Mediate(CancellationToken cancellationToken = default)
    {
      await using var bufferedRequestSource = new BufferedCommunicationSource(context.Source);
      var bufferedContext = new CommunicationSourceContext
      {
        Source = bufferedRequestSource,
        Sink = context.Sink
      };

      var retryCount = 1;

      using var communicationFailedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

      do
      {
        try
        {
          communicationFailedCts.TryReset();  
          LogRetry(retryCount);
          return await Mediate(bufferedContext, communicationFailedCts.Token);
        }
        catch (Exception ex)
        {
          LogInvocationError(ex);
          communicationFailedCts.Cancel();
        }
      } while (ServerCapabilities.FaultToleranceEnabled && retryCount++ < ServerCapabilities.MaxRetryAttempts);

      return false;
    }

    private void LogRetry(int retryCount)
    {
      if (!ServerCapabilities.VerboseLogging) return;
      if (!logger.IsEnabled(LogLevel.Warning)) return;
      if (retryCount == 1) return;

      logger.LogWarning("Retrying invocation for {WebSocketClientName}, method: {Method}. Attempt {RetryCount} of {MaxRetryAttempts}",
        targetDescription.Name,
        targetDescription.Method,
        retryCount,
        ServerCapabilities.MaxRetryAttempts);
    }

    private void LogInvocationError(Exception ex)
    {
      if (!logger.IsEnabled(LogLevel.Error)) return;

      logger.LogError(ex, "Invocation error for {WebSocketClientName}, method: {Method}. Error: {ErrorMessage}",
        targetDescription.Name,
        targetDescription.Method,
        ex.Message);
    }
  }
}