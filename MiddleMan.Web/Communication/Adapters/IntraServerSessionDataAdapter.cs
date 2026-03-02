using MiddleMan.Service.WebSocketClientInvocationSession;
using MiddleMan.Service.WebSocketClientInvocationSession.Enums;

namespace MiddleMan.Web.Communication.Adapters;

public class IntraServerSessionDataAdapter(IWebSocketClientInvocationSessionService clientInvocationSessionService,
 Guid session, SessionDataTypes dataType) : IDataWriterAdapter
{
  private readonly IWebSocketClientInvocationSessionService clientInvocationSessionService = clientInvocationSessionService;
  private readonly Guid session = session;
  private readonly SessionDataTypes dataType = dataType;

  public async IAsyncEnumerable<byte[]> Adapt()
  {
    byte[]? bytes;

    do
    {
      bytes = await clientInvocationSessionService.GetDistributedSessionData(session, dataType);
      if (bytes != null && bytes.Length > 0)
      {
        yield return bytes;
      }
    } while (bytes != null && bytes.Length > 0);
  }
}
