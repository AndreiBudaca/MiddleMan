using MiddleMan.Service.WebSocketClientInvocationSession;
using MiddleMan.Service.WebSocketClientInvocationSession.Enums;
using MiddleMan.Web.Communication.Adapters;

namespace MiddleMan.Web.Communication;

public class IntraServerCommunicationManager(IWebSocketClientInvocationSessionService clientInvocationSessionService)
{
  private readonly IWebSocketClientInvocationSessionService clientInvocationSessionService = clientInvocationSessionService;

  public Task WriteAsync(IDataWriterAdapter dataWriterAdapter, Guid correlation, SessionDataTypes dataType)
  {
    return WriteAsync(dataWriterAdapter.Adapt(), correlation, dataType);
  }

  public async Task WriteAsync(IAsyncEnumerable<byte[]> dataSource, Guid correlation, SessionDataTypes dataType)
  {
    await foreach (var bytes in dataSource)
    {
      await clientInvocationSessionService.SendSessionData(correlation, bytes, dataType);
    }

    await clientInvocationSessionService.CompleteSessionData(correlation, dataType);
  }
}
