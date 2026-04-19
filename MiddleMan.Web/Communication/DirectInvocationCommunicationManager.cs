using Microsoft.AspNetCore.SignalR;
using MiddleMan.Core.Extensions;
using MiddleMan.Web.Communication.ClientContracts;
using MiddleMan.Web.Communication.Metadata;

namespace MiddleMan.Web.Communication
{
  public class DirectInvocationCommunicationManager(IAsyncEnumerable<byte[]> body, HttpRequestMetadata metadata, bool sendMetadata = false)
  {
    public async Task<DirectInvocationResponse> InvokeAsync(ISingleClientProxy clientProxy, string method, CancellationToken cancellationToken)
    {
      var payload = new DirectInvocationData
      {
        Metadata = sendMetadata ? metadata : null,
        Data = await body.ReadAllBytes(cancellationToken)
      };

      return await clientProxy.InvokeAsync<DirectInvocationResponse>(method, payload, cancellationToken);
    }
  }
}