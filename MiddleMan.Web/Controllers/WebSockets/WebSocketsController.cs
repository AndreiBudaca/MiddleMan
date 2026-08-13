using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiddleMan.Service.WebSocketClients;
using MiddleMan.Web.Communication;
using MiddleMan.Web.Communication.Mediator;
using MiddleMan.Web.Communication.Resolver;
using MiddleMan.Web.Communication.Source;
using MiddleMan.Web.Communication.SourceSink;
using MiddleMan.Web.Controllers.ActionResults;
using MiddleMan.Web.Infrastructure.Attributes;
using MiddleMan.Web.Infrastructure.Identity;

namespace MiddleMan.Web.Controllers.WebSockets
{
  [Authorize]
  [Route("client-portal")]
  [DisableModelBinding]
  public class WebSocketsController(
    IWebSocketClientsService webSocketClientsService,
    ILogger<ICommunicationMediator> logger,
    ICommunicationTargetResolver targetResolver) : Controller
  {
    private readonly IWebSocketClientsService webSocketClientsService = webSocketClientsService;
    private readonly ILogger<ICommunicationMediator> logger = logger;
    private readonly ICommunicationTargetResolver targetResolver = targetResolver;

    [RequestSizeLimit(1_000_000_000)]
    [Route("{userId}/{webSocketClientName}/{method}/{*rest}")]
    public async Task Send([FromRoute] string userId, [FromRoute] string webSocketClientName, [FromRoute] string method, CancellationToken cancellationToken)
    {
      if (string.IsNullOrWhiteSpace(webSocketClientName) || string.IsNullOrWhiteSpace(method))
      {
        await new StatusResult(StatusCodes.Status400BadRequest).ApplyResultAsync(HttpContext);
        return;
      }

      if (HttpContext.Request.ContentLength == null && HttpContext.Request.Method != HttpMethods.Get)
      {
        await new StatusResult(StatusCodes.Status411LengthRequired).ApplyResultAsync(HttpContext);
        return;
      }

      if (userId != null && userId != User.Identifier())
      {
        var isAllowed = await webSocketClientsService.ExistsWebSocketClientShares(userId, webSocketClientName, User.Email());

        if (!isAllowed)
        {
          await new StatusResult(StatusCodes.Status403Forbidden).ApplyResultAsync(HttpContext);
          return;
        }
      }

      var mediator = new FaultTolerantMediator(
        logger,
        new CommunicationSourceContext
        {
          Source = new HttpRequestCommunicationSource(HttpContext.Request),
          Sink = new HttpResponseCommunicationSink(HttpContext.Response)
        },
        targetResolver,
        new TargetDescription
        {
          UserId = userId ?? User.Identifier(),
          Email = User.Email(),
          Name = webSocketClientName,
          Method = method
        }
      );

      var wasSuccessful = await mediator.Mediate(cancellationToken);
      if (wasSuccessful) return;

      await new StatusResult(StatusCodes.Status503ServiceUnavailable).ApplyResultAsync(HttpContext);
    }
  }
}
