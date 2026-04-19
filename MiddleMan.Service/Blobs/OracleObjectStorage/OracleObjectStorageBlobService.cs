using MiddleMan.Core;
using Oci.ObjectstorageService.Models;
using Oci.ObjectstorageService.Requests;

namespace MiddleMan.Service.Blobs.OracleObjectStorage
{
  public class OracleObjectStorageBlobService(OracleObjectStorageClientConfigurator configurator) : IBlobService
  {
    private readonly OracleObjectStorageClientConfigurator configurator = configurator;

    public Task DeleteBlob(string relativeUrl)
    {
      return configurator.Client.DeleteObject(new DeleteObjectRequest
      {
        BucketName = configurator.Bucket,
        ObjectName = relativeUrl,
        NamespaceName = configurator.Namespace
      });
    }

    public async Task<string> GetAbsoluteUrl(string relativeUrl)
    {
      var parRequest = new CreatePreauthenticatedRequestRequest
      {
        NamespaceName = configurator.Namespace,
        BucketName = configurator.Bucket,
        CreatePreauthenticatedRequestDetails = new CreatePreauthenticatedRequestDetails
        {
          Name = "my-par",
          ObjectName = relativeUrl,
          AccessType = CreatePreauthenticatedRequestDetails.AccessTypeEnum.ObjectRead,
          TimeExpires = DateTime.UtcNow.AddMinutes(5),
        }
      };

      var parResponse = await configurator.Client.CreatePreauthenticatedRequest(parRequest);

      string publicUrl = parResponse.PreauthenticatedRequest.AccessUri;
      return $"https://objectstorage.{configurator.Region}.oraclecloud.com{publicUrl}";
    }

    public async Task<string> UploadBlob(string[] blobParts, IAsyncEnumerable<byte[]> data, CancellationToken cancellationToken)
    {
      var tempFilePath = Path.GetTempFileName();

      try
      {
        using var blobStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, ServerCapabilities.MaxChunkSize, useAsync: true);
        await foreach (var b in data.WithCancellation(cancellationToken))
        {
          await blobStream.WriteAsync(b.AsMemory(0, b.Length), cancellationToken);
        }
        blobStream.Position = 0;

        var blobPath = string.Join('/', blobParts);
        var request = new PutObjectRequest
        {
          BucketName = configurator.Bucket,
          ObjectName = blobPath,
          NamespaceName = configurator.Namespace,
          PutObjectBody = blobStream,
        };

        var response = await configurator.Client.PutObject(request, cancellationToken: cancellationToken);

        return blobPath;
      }
      finally
      {
        if (File.Exists(tempFilePath))
        {
          File.Delete(tempFilePath);
        }
      }
    }

    public async Task<string> UploadBlob(string[] blobParts, byte[] data, CancellationToken cancellationToken)
    {
      var blobPath = string.Join('/', blobParts);
      var request = new PutObjectRequest
      {
        BucketName = configurator.Bucket,
        ObjectName = blobPath,
        NamespaceName = configurator.Namespace,
        PutObjectBody = new MemoryStream(data),
      };

      var response = await configurator.Client.PutObject(request, cancellationToken: cancellationToken);

      return blobPath;
    }
  }
}