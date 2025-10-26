using MiddleMan.Core;
using MiddleMan.Core.Extensions;
using MiddleMan.Service.Blobs;
using MiddleMan.Service.WebSocketClientMethods.Classes;
using MiddleMan.Service.WebSocketClientMethods.Constants;

namespace MiddleMan.Service.WebSocketClientMethods
{
  public class WebSocketClientMethodService(IBlobService blobService) : IWebSocketClientMethodService
  {
    private readonly IBlobService _blobService = blobService;

    private static Dictionary<string, byte[]> _methodSignaturesCache = new();

    public async Task ReceiveMethodsAsync(string identifier, string name, IAsyncEnumerable<byte[]> methodChunks, CancellationToken cancellationToken)
    {
      var key = $"{identifier}_{name}";

      var metadataBytes = await methodChunks.EnumerateUntil(4, 0, cancellationToken);

      var metadata = GetMetadata(metadataBytes.Received);
      if (!ServerCapabilities.AllowedVersions.Contains(metadata.Version)) throw new NotSupportedException($"Version {metadata.Version} is not supported.");
      if (metadata.Operation == MethodPackConstants.Operations.OK) return;

      var signatureBytes = await methodChunks.PrependItems(cancellationToken, metadataBytes.CurrentEnumerationItem)
        .EnumerateUntil(metadata.MethodCount * 32, 0, cancellationToken);

      SaveSignatures(key, signatureBytes.Received);

      await _blobService.UploadBlob("websocket-client-methods", $"{identifier}_{name}_methods.bin",
        methodChunks.PrependItems(cancellationToken, [metadata.Version], BitConverter.GetBytes(metadata.MethodCount), signatureBytes.CurrentEnumerationItem), cancellationToken);
    }

    private static MethodMetadataDto GetMetadata(byte[] metadataBytes)
    {
      return new MethodMetadataDto
      {
        Version = metadataBytes[0],
        Operation = metadataBytes[1],
        MethodCount = BitConverter.ToInt16(metadataBytes, 2)
      };
    }

    private static void SaveSignatures(string key, byte[] bytes)
    {
      _methodSignaturesCache[key] = bytes;
    }
  }
}
