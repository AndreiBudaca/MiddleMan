using MiddleMan.Core;

namespace MiddleMan.Service.Blobs
{
  public interface IBlobService
  {
    Task<string> GetAbsoluteUrl(string relativeUrl);

    Task<string> UploadBlob(string[] blobParts, IAsyncEnumerable<DataChunk> data, CancellationToken cancellationToken);

    Task<string> UploadBlob(string[] blobParts, byte[] data, CancellationToken cancellationToken);

    Task DeleteBlob(string relativeUrl);
  }
}
