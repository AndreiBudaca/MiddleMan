using Oci.Common;
using Oci.Common.Auth;
using Oci.ObjectstorageService;

namespace MiddleMan.Service.Blobs.OracleObjectStorage
{
  public class OracleObjectStorageClientConfigurator
  {
    public ObjectStorageClient Client { get; }
    public string Region { get; }
    public string Namespace { get; }
    public string Bucket { get; }

    public OracleObjectStorageClientConfigurator(string user, string fingerprint, string tenancy, string region, string keyFile, string @namespace, string bucket)
    {
      var provider = new SimpleAuthenticationDetailsProvider
      {
        TenantId = tenancy,
        UserId = user,
        Fingerprint = fingerprint,
        Region = Oci.Common.Region.FromRegionId(region),
        PrivateKeySupplier = new FilePrivateKeySupplier(keyFile, null),
      };

      Client = new ObjectStorageClient(provider, new ClientConfiguration());
      Region = region;
      Namespace = @namespace;
      Bucket = bucket;
    }
  }
}