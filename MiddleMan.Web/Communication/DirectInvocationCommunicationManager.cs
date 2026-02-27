using Microsoft.AspNetCore.SignalR;
using MiddleMan.Core;
using MiddleMan.Web.Communication.ClientContracts;
using MiddleMan.Web.Communication.Metadata;

namespace MiddleMan.Web.Communication
{
  public class DirectInvocationCommunicationManager(HttpRequest request, HttpUser? user, bool sendMetadata = false)
  {
    private readonly byte[] metadataBytes = sendMetadata ? new HttpRequestMetadata(request, user).SerializeJson() : [];

    public bool IsLengthValid()
    {
      return request.ContentLength != null && (request.ContentLength + metadataBytes.Length <= ServerCapabilities.MaxContentLength);
    }

    public async Task<DirectInvocationResponse> InvokeAsync(ISingleClientProxy clientProxy, string method, CancellationToken cancellationToken)
    {
      var buffer = new byte[request.ContentLength ?? 0];
      await request.Body.ReadAsync(buffer, cancellationToken);

      var payload = new DirectInvocationData
      {
        Metadata = sendMetadata ? new HttpRequestMetadata(request, user) : null,
        Data = buffer
      };

      return await clientProxy.InvokeAsync<DirectInvocationResponse>(method, payload, cancellationToken);
    }
  }
}