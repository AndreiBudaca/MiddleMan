namespace MiddleMan.Core
{
  public static class ServerCapabilities
  {
    public const int MaxContentLength = 64 * 1024; // 32KB
    
    public const int IntraServerBufferedChunks = 1;

    public const int GlobalTimeoutSeconds = int.MaxValue / 1000 - 1;

    public const int ClientConnectionTimeoutSeconds = 5;

    public static readonly int[] AllowedVersions = [0];

    public static string StaticFilesPath => Environment.GetEnvironmentVariable("LOCAL_BLOB_PATH") ?? $"{Directory.GetCurrentDirectory()}/blobs";

    public static string UIStaticFilesPath => $"{Directory.GetCurrentDirectory()}/ClientApp/build/client";
  }
}
