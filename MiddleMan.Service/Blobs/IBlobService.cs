namespace MiddleMan.Service.Blobs
{
  public interface IBlobService
  {
    Task UploadBlob(string container, string blob, IAsyncEnumerable<byte[]> data, CancellationToken cancellationToken);

    void DeleteBlob(string container, string blob);
  }
}
