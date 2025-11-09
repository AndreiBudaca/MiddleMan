using MiddleMan.Service.Blobs.Dto;

namespace MiddleMan.Service.Blobs
{
  public class LocalFileSystemBlobService() : IBlobService
  {
    private const int BufferSize = 4096;

    public async Task<BlobInfoDto> UploadBlob(string container, string blob, IAsyncEnumerable<byte[]> dataChunks, CancellationToken cancellationToken)
    {
      var filePath = Path.Combine(container, blob);

      using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);

      await foreach (var b in dataChunks.WithCancellation(cancellationToken))
      {
        await fileStream.WriteAsync(b.AsMemory(0, b.Length), cancellationToken);
      }

      return new BlobInfoDto
      {
        AbsoluteUrl = $"{container}/{blob}",
        RelativeUrl = blob,
      };
    }

    public Task DeleteBlob(string container, string blob)
    {
      var filePath = Path.Combine(container, blob);
      File.Delete(filePath);

      return Task.CompletedTask;
    }
  }
}
