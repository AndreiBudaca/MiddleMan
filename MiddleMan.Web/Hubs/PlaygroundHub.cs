using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MiddleMan.Communication;
using MiddleMan.Core;
using MiddleMan.Core.Extensions;
using MiddleMan.Service.WebSocketClientConnections;
using MiddleMan.Service.WebSocketClientConnections.Classes;
using MiddleMan.Service.WebSocketClientMethods;
using MiddleMan.Service.WebSocketClients;
using MiddleMan.Web.Communication;
using MiddleMan.Web.Communication.ClientContracts;
using MiddleMan.Web.Communication.CommunicationSync.SameServer;
using MiddleMan.Web.Communication.Mediator;
using MiddleMan.Web.Communication.Metadata;
using MiddleMan.Web.Communication.Metadata.Constants;
using MiddleMan.Web.Communication.Resolver;
using MiddleMan.Web.Communication.Source;
using MiddleMan.Web.Communication.SourceSink;
using MiddleMan.Web.Communication.Target;
using MiddleMan.Web.Hubs.Models;
using MiddleMan.Web.Infrastructure.Identity;
using System.Threading.Channels;

namespace MiddleMan.Web.Hubs
{
  [Authorize]
  public class PlaygroundHub(IWebSocketClientsService webSocketClientsService,
    IWebSocketClientMethodService webSocketClientMethodService,
    ClientInfoCommunicationManager clientInfoCommunicationManager,
    IntraServerCommunicationManager intraServerCommunicationManager,
    IWebSocketClientConnectionsService webSocketClientConnectionsService,
    ILogger<PlaygroundHub> logger) : Hub
  {
    private readonly IWebSocketClientsService webSocketClientsService = webSocketClientsService;
    private readonly IWebSocketClientConnectionsService webSocketClientConnectionsService = webSocketClientConnectionsService;
    private readonly IWebSocketClientMethodService webSocketClientMethodService = webSocketClientMethodService;
    private readonly ClientInfoCommunicationManager clientInfoCommunicationManager = clientInfoCommunicationManager;
    private readonly IntraServerCommunicationManager intraServerCommunicationManager = intraServerCommunicationManager;
    private readonly ILogger<PlaygroundHub> logger = logger;

    #region [Clinet connection hooks]
    public override async Task OnConnectedAsync()
    {
      var (id, name, _, clientToken, clientId) = GetClientInfoFromContext();

      if (string.IsNullOrWhiteSpace(clientId))
      {
        logger.LogWarning("Client connection attempt without client identity header. Connection ID: {ConnectionId}", Context.ConnectionId);
        Context.Abort();
        return;
      }

      var isAuthorized = await AuthorizeClient(id, name, clientToken);
      if (!isAuthorized)
      {
        Context.Abort();
        return;
      }

      var onServerConnectionCount = webSocketClientConnectionsService.AddWebSocketClientConnection(id, name, clientId, Context.ConnectionId);
      if (onServerConnectionCount == 1 && ServerCapabilities.ClusterMode)
      {
        await clientInfoCommunicationManager.StartListening(id, name);
      }

      logger.LogInformation("Client connected: {Id} - {Name}. Assigning connection ID: {ConnectionId}. Total connections: {TotalConnections}", id, name, Context.ConnectionId, onServerConnectionCount);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
      var (id, name, _, _, clientId) = GetClientInfoFromContext();

      if (string.IsNullOrWhiteSpace(clientId))
      {
        logger.LogWarning("Client disconnection without client identity header. Connection ID: {ConnectionId}", Context.ConnectionId);
        return;
      }

      var onServerConnectionCount = webSocketClientConnectionsService.DeleteWebSocketClientConnection(id, name, clientId);
      if (onServerConnectionCount == 0 && ServerCapabilities.ClusterMode)
      {
        await clientInfoCommunicationManager.StopListening(id, name);
      }

      logger.LogInformation("Client disconnected: {Id} - {Name}. Connection ID: {ConnectionId}. Total connections: {TotalConnections}", id, name, Context.ConnectionId, onServerConnectionCount);
    }

    private async Task<bool> AuthorizeClient(string id, string name, string? clientToken)
    {
      if (clientToken == null || !clientToken.StartsWith("Bearer ")) return false;
      return await webSocketClientsService.IsValidWebSocketClientToken(id, name, clientToken[7..]);
    }
    #endregion

    #region [Server info methods]
    public async Task Methods(ChannelReader<byte[]> channelReader)
    {
      var (id, name, _, _, clientId) = GetClientInfoFromContext();

      await ConnectionChecks(id, name, clientId);

      await webSocketClientMethodService.ReceiveMethodsAsync(id, name, channelReader.AsDataChunkChannelReader().ReadAllAsync(), CancellationToken.None);
    }

    public async Task MethodsRaw(byte[] methodsBytes)
    {
      var (id, name, _, _, clientId) = GetClientInfoFromContext();

      await ConnectionChecks(id, name, clientId);

      await webSocketClientMethodService.ReceiveMethodsAsync(id, name, methodsBytes, CancellationToken.None);
    }

    public async Task<ServerInfoModel> Negociate(ClientInfoModel clientInfo)
    {
      var (id, name, _, _, clientId) = GetClientInfoFromContext();
      await ConnectionChecks(id, name, clientId);

      if (clientInfo == null) return new ServerInfoModel { IsAccepted = false };
      if (!ServerCapabilities.AllowedVersions.Contains(clientInfo.Version)) return new ServerInfoModel { IsAccepted = false };

      webSocketClientConnectionsService.AddWebSockerClientConnectionCapabilities(
        id,
        name,
        clientId!,
        new ClientCapabilities
        {
          Version = clientInfo.Version,
          SupportsStreaming = clientInfo.SupportsStreaming,
          SendHTTPMetadata = clientInfo.SendHTTPMetadata,
        }
      );

      var client = await webSocketClientsService.GetWebSocketClient(id, name);
      return new ServerInfoModel
      {
        IsAccepted = true,
        MaxMessageLength = ServerCapabilities.MaxChunkSize,
        MethodSignature = client?.Signature,
      };
    }
    #endregion

    #region [Communication methods]
    public async Task AddReadChannel(Guid correlation, HttpResponseMetadata? metadata, ChannelReader<byte[]> channelReader)
    {
      var (id, name, _, _, clientId) = GetClientInfoFromContext();
      await ConnectionChecks(id, name, clientId);

      var session = CommunicationSessionRegistry.GetSession(correlation)
        ?? throw new HubException("Communication session not found");

      session.ResponseMetadataSource.TrySetResult(metadata);
      session.ChannelReaderSource.TrySetResult(channelReader.AsDataChunkChannelReader());

      await session.ReaderCompletion;
    }

    public async IAsyncEnumerable<Memory<byte>> SubscribeToServer(Guid correlation)
    {
      var (id, name, _, _, clientId) = GetClientInfoFromContext();
      await ConnectionChecks(id, name, clientId);

      // Create channel
      var channel = Channel.CreateBounded<DataChunk>(new BoundedChannelOptions(1));

      // Try get exsiting session
      var session = CommunicationSessionRegistry.GetSession(correlation);

      // Mark session state and create new control if needed
      var sessionWasMissing = session == null;
      var control = sessionWasMissing ? CommunicationSessionRegistry.RegisterSession(correlation) : null;

      // Resolve the writer TaskCompletionSource and register the writer
      var channelWriterSource = session?.ChannelWriterSource ?? control?.ChannelWriterSource;
      channelWriterSource?.TrySetResult(channel.Writer);

      // If there was no session, the hub is responsible to control the communication.
      // Since we need to return data to the client, we will handle the communication on a background Task
      Task? multiServerCommunicationTask = null;
      if (sessionWasMissing)
      {
        // TO DO: Use custom exceptions
        multiServerCommunicationTask = Task.Run(() => InitiateStreamingCommunication(correlation,
          control ?? throw new InvalidOperationException("Control can't be null when initializing client communication")));
      }

      // Consume the channel and yield the data to the client
      // TO DO: add a timeout for the channel reading to avoid hanging indefinitely if data isn't received
      try
      {
        await foreach (var chunk in channel.Reader.ReadAllAsync())
        {
          yield return chunk.Data.AsMemory(0, chunk.Length);
        }
      }
      finally
      {
        if (multiServerCommunicationTask != null) await multiServerCommunicationTask;
      }
    }

    // public async Task<DirectInvocationResponse> Invoke(string? userId, string clientName, string method, DirectInvocationData clientData)
    // {
    //   var (id, _, email, _, _) = GetClientInfoFromContext();

    //   if (userId != null && userId != id)
    //   {
    //     var isAllowed = await webSocketClientsService.ExistsWebSocketClientShares(userId, clientName, email);
    //     if (!isAllowed) throw new HubException("Not allowed to invoke this method");
    //   }

    //   var retryCount = 1;
    //   var wasSuccessfulInvocation = false;
    //   using var communicationFailedCts = new CancellationTokenSource();

    //   do
    //   {
    //     if (retryCount > 1)
    //     {
    //       logger.LogWarning("Retrying invocation for {WebSocketClientName}, method: {Method}. Attempt {RetryCount} of {MaxRetryAttempts}", clientName, method, retryCount, ServerCapabilities.MaxRetryAttempts);
    //     }

    //     communicationFailedCts.TryReset();
    //     var clientConnection = await GetClientConnection(userId ?? id, clientName, communicationFailedCts.Token);

    //     if (string.IsNullOrWhiteSpace(clientConnection?.ConnectionId)) throw new HubException("Client connection not found");

    //     var hubClient = Clients.Client(clientConnection.ConnectionId);
    //     if (hubClient == null)
    //     {
    //       webSocketClientConnectionsService.DeleteWebSocketClientConnection(userId ?? id, clientName, clientConnection.ConnectionId);
    //       // TO DO: also delete from other servers in cluster
    //       throw new HubException("Client connection not found");
    //     }

    //     if (ServerCapabilities.VerboseLogging)
    //     {
    //       logger.LogInformation("Invoking {WebSocketClientName}, method: {Method}. Connection ID: {ConnectionId}", clientName, method, clientConnection.ConnectionId);
    //     }

    //     var invoker = new DirectClientInvoker(logger);

    //     try
    //     {
    //       var (responseMetadata, responseData) = await invoker.Invoke(clientData.Data.AsAsyncEnumerable(), ProcessMetadata(clientData, Context), method, clientConnection, hubClient, communicationFailedCts.Token);
    //       await invoker.Cleanup();

    //       if (ServerCapabilities.VerboseLogging)
    //       {
    //         logger.LogInformation("Completed invocation for {WebSocketClientName}, method: {Method}. Connection ID: {ConnectionId}", clientName, method, clientConnection.ConnectionId);
    //       }

    //       return new DirectInvocationResponse
    //       {
    //         Metadata = clientConnection.ClientCapabilities.SendHTTPMetadata ? responseMetadata : null,
    //         Data = responseData != null ? await responseData.ReadAllBytes(communicationFailedCts.Token) : []
    //       };
    //     }
    //     catch (Exception ex)
    //     {
    //       logger.LogError("Invocation error for {WebSocketClientName}, method: {Method}. Connection ID: {ConnectionId}. Error: {ErrorMessage}", clientName, method, clientConnection.ConnectionId, ex.Message);

    //       await communicationFailedCts.CancelAsync();
    //       await invoker.Cleanup();
    //     }
    //   } while (!wasSuccessfulInvocation && ServerCapabilities.FaultToleranceEnabled && retryCount++ < ServerCapabilities.MaxRetryAttempts);

    //   throw new HubException("Invocation failed after multiple attempts");
    // }

    // public async Task Send(string? userId, string clientName, string method, DirectInvocationData clientData)
    // {
    //   var (id, _, email, _, _) = GetClientInfoFromContext();

    //   if (userId != null && userId != id)
    //   {
    //     var isAllowed = await webSocketClientsService.ExistsWebSocketClientShares(userId, clientName, email);
    //     if (!isAllowed) throw new HubException("Not allowed to invoke this method");
    //   }

    //   var retryCount = 1;
    //   var wasSuccessfulInvocation = false;
    //   using var communicationFailedCts = new CancellationTokenSource();

    //   do
    //   {
    //     if (retryCount > 1)
    //     {
    //       logger.LogWarning("Retrying invocation for {WebSocketClientName}, method: {Method}. Attempt {RetryCount} of {MaxRetryAttempts}", clientName, method, retryCount, ServerCapabilities.MaxRetryAttempts);
    //     }

    //     communicationFailedCts.TryReset();
    //     var clientConnection = await GetClientConnection(userId ?? id, clientName, communicationFailedCts.Token);

    //     if (string.IsNullOrWhiteSpace(clientConnection?.ConnectionId)) throw new HubException("Client connection not found");

    //     var hubClient = Clients.Client(clientConnection.ConnectionId);
    //     if (hubClient == null)
    //     {
    //       webSocketClientConnectionsService.DeleteWebSocketClientConnection(userId ?? id, clientName, clientConnection.ConnectionId);
    //       // TO DO: also delete from other servers in cluster
    //       throw new HubException("Client connection not found");
    //     }

    //     if (ServerCapabilities.VerboseLogging)
    //     {
    //       logger.LogInformation("Invoking {WebSocketClientName}, method: {Method}. Connection ID: {ConnectionId}", clientName, method, clientConnection.ConnectionId);
    //     }

    //     var invoker = new DirectClientInvoker(logger);

    //     try
    //     {
    //       await invoker.Send(clientData.Data.AsAsyncEnumerable(), ProcessMetadata(clientData, Context), method, clientConnection, hubClient, communicationFailedCts.Token);
    //       await invoker.Cleanup();

    //       if (ServerCapabilities.VerboseLogging)
    //       {
    //         logger.LogInformation("Completed invocation for {WebSocketClientName}, method: {Method}. Connection ID: {ConnectionId}", clientName, method, clientConnection.ConnectionId);
    //       }

    //       return;
    //     }
    //     catch (Exception ex)
    //     {
    //       logger.LogError("Invocation error for {WebSocketClientName}, method: {Method}. Connection ID: {ConnectionId}. Error: {ErrorMessage}", clientName, method, clientConnection.ConnectionId, ex.Message);

    //       await communicationFailedCts.CancelAsync();
    //       await invoker.Cleanup();
    //     }
    //   } while (!wasSuccessfulInvocation && ServerCapabilities.FaultToleranceEnabled && retryCount++ < ServerCapabilities.MaxRetryAttempts);

    //   throw new HubException("Invocation failed after multiple attempts");
    // }

    public string Ping(string message)
    {
      if (message != "Ping") throw new HubException("Invalid ping message");
      return "Pong";
    }
    #endregion

    // TO DO: Refactor this to a hub filter
    private async Task ConnectionChecks(string id, string name, string? clientId)
    {
      if (string.IsNullOrWhiteSpace(clientId))
      {
        logger.LogWarning("Client method invocation attempt without client identity header. Connection ID: {ConnectionId}", Context.ConnectionId);
        Context.Abort();
        return;
      }

      var clientConectionExists = webSocketClientConnectionsService.ExistsWebSocketClientConnection(id, name, clientId);

      if (!clientConectionExists)
      {
        Context.Abort();
      }
    }

    private (string, string, string, string?, string?) GetClientInfoFromContext()
    {
      var id = Context.User!.Identifier();
      var name = Context.User!.Name();
      var email = Context.User!.Email();
      var clientToken = Context.GetHttpContext()?.Request.Headers.Authorization.FirstOrDefault();
      var clientId = Context.GetHttpContext()?.Request.Headers["X-Client-Identity"].FirstOrDefault();

      return (id, name, email, clientToken, clientId);
    }

    private async Task InitiateStreamingCommunication(Guid correlation, CommunicationControl control)
    {
      var mediator = new SimpleMediator(
        logger,
        new CommunicationSourceContext
        {
          Source = new ChannelCommunicationSource(correlation, intraServerCommunicationManager),
          Sink = new ChannelCommunicationSink(correlation, intraServerCommunicationManager)
        },
        new ManualTargetResolver(
          new SameServerWebSocketStreamingInvocationTarget(correlation, control)
        ),
        new TargetDescription()
      );

      // TO DO: see if you can resolve a token
      await mediator.Mediate(CancellationToken.None);
    }

    private static HttpRequestMetadata ProcessMetadata(DirectInvocationData requestData, HubCallerContext context)
    {
      var user = new HttpUser
      {
        Identifier = context.User!.Identifier(),
        Role = UserTypes.Client
      };

      if (requestData.Metadata != null)
      {
        requestData.Metadata.User = user;
        return requestData.Metadata;
      }

      return new HttpRequestMetadata
      {
        User = user,
      };
    }
  }
}