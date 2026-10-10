using Microsoft.AspNetCore.SignalR;
using MiddleMan.Core;
using MiddleMan.Core.Extensions;
using MiddleMan.Web.Communication.ClientContracts;


namespace MiddleMan.Web.Communication.Target
{
  public class DirectInvocationTarget(ISingleClientProxy clientProxy, string method) : ICommunicationTarget
  {
    private readonly ISingleClientProxy clientProxy = clientProxy;
    private readonly string method = method;

    public async Task<ResponseData?> WriteData(RequestData? data, CancellationToken cancellationToken = default)
    {
      var payload = new DirectInvocationData
      {
        Metadata = data?.Metadata,
        Data = data?.Data != null ? await data.Data.ReadAllBytes(maxLength: ServerCapabilities.MaxChunkSize, cancellationToken) : []
      };

      var response = await clientProxy.InvokeAsync<DirectInvocationResponse>(method, payload, cancellationToken);

      var responseChunk = new DataChunk
      {
        Data = response.Data,
        Length = response.Data.Length
      };

      return new ResponseData
      {
        Metadata = response.Metadata,
        Data = responseChunk.AsAsyncEnumerable()
      };
    }
  }
}