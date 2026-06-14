using Microsoft.AspNetCore.SignalR;
using MiddleMan.Core.Extensions;
using MiddleMan.Web.Communication.ClientContracts;
using MiddleMan.Web.Communication.Metadata;

namespace MiddleMan.Web.Communication
{
  public class DirectInvocationCommunicationManager(IAsyncEnumerable<byte[]> body, HttpRequestMetadata metadata, bool sendMetadata = false)
  {
    public async Task SendAsync(ISingleClientProxy clientProxy, string method, CancellationToken cancellationToken)
    {
      var payload = await CreatePayload(cancellationToken);
      await clientProxy.SendAsync(method, payload, cancellationToken);
    }

    public async Task<DirectInvocationResponse> InvokeAsync(ISingleClientProxy clientProxy, string method, CancellationToken cancellationToken)
    {
      var payload = await CreatePayload(cancellationToken);
      return await clientProxy.InvokeAsync<DirectInvocationResponse>(method, payload, cancellationToken);
    }

    private async Task<DirectInvocationData> CreatePayload(CancellationToken cancellationToken)
    {
      return new DirectInvocationData
      {
        Metadata = sendMetadata ? metadata : null,
        Data = await body.ReadAllBytes(cancellationToken)
      };
    }
  }
}