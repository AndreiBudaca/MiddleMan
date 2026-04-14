using MiddleMan.Core;

namespace MiddleMan.Service.Blobs
{
  public class LocalFileSystemBlobService() : IBlobService
  {
    public async Task<string> UploadBlob(string[] blobParts, IAsyncEnumerable<byte[]> dataChunks, CancellationToken cancellationToken)
    {
      var blobPath = string.Join(Path.DirectorySeparatorChar, blobParts);
      var filePath = Path.Combine(ServerCapabilities.StaticFilesPath, blobPath);

      using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, ServerCapabilities.MaxContentLength, useAsync: true);

      await foreach (var b in dataChunks.WithCancellation(cancellationToken))
      {
        await fileStream.WriteAsync(b.AsMemory(0, b.Length), cancellationToken);
      }

      return blobPath;
    }

    public async Task<string> UploadBlob(string[] blobParts, byte[] data, CancellationToken cancellationToken)
    {
      var blobPath = string.Join(Path.DirectorySeparatorChar, blobParts);
      var filePath = Path.Combine(ServerCapabilities.StaticFilesPath, blobPath);

      using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, ServerCapabilities.MaxContentLength, useAsync: true);
      await fileStream.WriteAsync(data.AsMemory(0, data.Length), cancellationToken);

      return blobPath;
    }

    public Task DeleteBlob(string relativeUrl)
    {
      var filePath = Path.Combine(ServerCapabilities.StaticFilesPath, relativeUrl);
      File.Delete(filePath);

      return Task.CompletedTask;
    }

    public Task<string> GetAbsoluteUrl(string relativeUrl)
    {
      return Task.FromResult($"{ServerCapabilities.StaticFilesPath}/{relativeUrl}".Replace(Path.DirectorySeparatorChar, '/'));
    }
  }
}
