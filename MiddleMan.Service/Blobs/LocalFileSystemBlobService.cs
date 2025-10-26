namespace MiddleMan.Service.Blobs
{
  public class LocalFileSystemBlobService() : IBlobService
  {
    private const int BufferSize = 4096;

    public async Task UploadBlob(string container, string blob, IAsyncEnumerable<byte[]> dataChunks, CancellationToken cancellationToken)
    {
      Directory.CreateDirectory(Path.Combine(AbsolutePath, container));
      var filePath = Path.Combine(AbsolutePath, container, blob);

      using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);

      await foreach (var b in dataChunks.WithCancellation(cancellationToken))
      {
        await fileStream.WriteAsync(b.AsMemory(0, b.Length), cancellationToken);
      }
    }

    public void DeleteBlob(string container, string blob)
    {
      throw new NotImplementedException();
    }

    private static string AbsolutePath => Environment.GetEnvironmentVariable("LOCAL_BLOB_PATH") ?? Directory.GetCurrentDirectory();
  }
}
