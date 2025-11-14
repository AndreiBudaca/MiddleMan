using MiddleMan.Core;
using MiddleMan.Core.Extensions;
using MiddleMan.Data.Persistency;
using MiddleMan.Data.Persistency.Classes;
using MiddleMan.Data.Persistency.Entities;
using MiddleMan.Service.Blobs;
using MiddleMan.Service.WebSocketClientMethods.Classes;
using MiddleMan.Service.WebSocketClientMethods.Constants;

namespace MiddleMan.Service.WebSocketClientMethods
{
  public class WebSocketClientMethodService(IClientRepository clientRepository, IBlobService blobService) : IWebSocketClientMethodService
  {
    private readonly IClientRepository _clientRepository = clientRepository;
    private readonly IBlobService _blobService = blobService;

    public async Task ReceiveMethodsAsync(string identifier, string name, IAsyncEnumerable<byte[]> methodChunks, CancellationToken cancellationToken)
    {
      var client = await _clientRepository.GetByIdAsync((identifier, name)) ?? throw new InvalidOperationException($"Client {identifier}:{name} not found.");

      var metadataBytes = await methodChunks.EnumerateUntil(35, 0, cancellationToken);

      var metadata = GetMetadata(metadataBytes.Received);
      if (!ServerCapabilities.AllowedVersions.Contains(metadata.Version)) throw new NotSupportedException($"Version {metadata.Version} is not supported.");
      if (metadata.Operation == MethodPackConstants.Operations.OK) return;

      var newFileInfo = await _blobService.UploadBlob(ServerCapabilities.StaticFilesPath, $"websocket-client-methods{Path.DirectorySeparatorChar}{identifier}_{name}_methods_{Guid.NewGuid()}.bin",
        methodChunks.PrependItems(cancellationToken, metadataBytes.Received, metadataBytes.CurrentEnumerationItem), cancellationToken);

      if (client?.MethodInfoUrl is not null)
      {
        await _blobService.DeleteBlob(ServerCapabilities.StaticFilesPath, client.MethodInfoUrl);
      }

      await _clientRepository.UpdateAsync((identifier, name),
       [
         new ColumnInfo
          {
            ColumnName = Client.Columns.Signatures,
            Value = metadata.Signature,
          },
           new ColumnInfo
          {
            ColumnName = Client.Columns.MethodInfoUrl,
            Value = newFileInfo.RelativeUrl,
          },
        ]);
    }

    private static MethodMetadataDto GetMetadata(byte[] metadataBytes)
    {
      return new MethodMetadataDto
      {
        Version = metadataBytes[0],
        Operation = metadataBytes[1],
        Signature = [.. metadataBytes.Skip(2).Take(32)],
        MethodCount = metadataBytes[34],
      };
    }
  }
}
