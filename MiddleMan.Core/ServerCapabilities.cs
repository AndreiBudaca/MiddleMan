namespace MiddleMan.Core
{
  public static class ServerCapabilities
  {
    public const int MaxContentLength = 32 * 1024; // 32KB
    
    public const int IntraServerBufferedChunks = 10;

    public const int GlobalTimeoutSeconds = 10;

    public const int ClientConnectionTimeoutSeconds = 5;

    public const bool ClusterMode = true;

    public static readonly int[] AllowedVersions = [0];

    public static string StaticFilesPath => Environment.GetEnvironmentVariable("LOCAL_BLOB_PATH") ?? $"{Directory.GetCurrentDirectory()}/blobs";

    public static string UIStaticFilesPath => $"{Directory.GetCurrentDirectory()}/ClientApp/build/client";
  }
}
