using MiddleMan.Core;
using MiddleMan.Data.InMemory;
using MiddleMan.Service.WebSocketClientInvocationSession.Enums;

namespace MiddleMan.Service.WebSocketClientInvocationSession
{
  public class WebSocketClientInvocationSessionService(IInMemoryContext inMemoryContext,
   ISharedInMemoryContext sharedInMemoryContext) : IWebSocketClientInvocationSessionService
  {
    private readonly IInMemoryContext inMemoryContext = inMemoryContext;
    private readonly ISharedInMemoryContext sharedInMemoryContext = sharedInMemoryContext;

    private const string SessionsHashKey = "sessions";
    private static string GetBoundedListKey(Guid correlation, SessionDataTypes dataType)
    {
      return dataType switch
      {
        SessionDataTypes.Request => $"Invocations:{correlation}",
        SessionDataTypes.Response => $"Responses:{correlation}",
        _ => throw new ArgumentException("Invalid session data type", nameof(dataType)),
      };
    }

    public async Task ClearSession(Guid correlation)
    {
      await inMemoryContext.RemoveFromHash(SessionsHashKey, correlation.ToString());
      await sharedInMemoryContext.TerminateBoundedList(GetBoundedListKey(correlation, SessionDataTypes.Request));
      await sharedInMemoryContext.TerminateBoundedList(GetBoundedListKey(correlation, SessionDataTypes.Response));
    }

    public async Task RegisterSession(Guid correlation)
    {
      await inMemoryContext.AddToHash<byte>(SessionsHashKey, correlation.ToString(), 1);
      await sharedInMemoryContext.CreateBoundedList(GetBoundedListKey(correlation, SessionDataTypes.Request), ServerCapabilities.IntraServerBufferedChunks);
      await sharedInMemoryContext.CreateBoundedList(GetBoundedListKey(correlation, SessionDataTypes.Response), ServerCapabilities.IntraServerBufferedChunks);
    }

    public async Task<bool> ExistsSession(Guid correlation)
    {
      var session = await inMemoryContext.GetFromHash<byte>(SessionsHashKey, correlation.ToString());
      return session != default;
    }

    public Task<byte[]?> GetDistributedSessionData(Guid correlation, SessionDataTypes dataType)
    {
      return sharedInMemoryContext.GetRawBytesFromBoundedList(GetBoundedListKey(correlation, dataType));
    }

    public Task SendSessionData(Guid correlation, byte[] data, SessionDataTypes dataType)
    {
      return sharedInMemoryContext.AddRawBytesToBoundedList(GetBoundedListKey(correlation, dataType), data);
    }

    public Task CompleteSessionData(Guid correlation, SessionDataTypes dataType)
    {
      return sharedInMemoryContext.AddRawBytesToBoundedList(GetBoundedListKey(correlation, dataType), []);
    }
  }
}