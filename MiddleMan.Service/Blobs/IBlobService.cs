namespace MiddleMan.Service.Blobs
{
  public interface IBlobService
  {
    Task<BlobInfoDto> UploadBlob(string container, string blob, IAsyncEnumerable<byte[]> data, CancellationToken cancellationToken);

    Task DeleteBlob(string container, string blob);
  }
}
