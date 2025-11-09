namespace MiddleMan.Core
{
  public static class ServerCapabilities
  {
    public const int MaxContentLength = 4 * 1024; // 4KB

    public static readonly int[] AllowedVersions = [0];

    public static string StaticFilesPath => Environment.GetEnvironmentVariable("LOCAL_BLOB_PATH") ?? Directory.GetCurrentDirectory();

    public static string UIStaticFilesPath => $"{StaticFilesPath}{Path.DirectorySeparatorChar}ClientApp{Path.DirectorySeparatorChar}build{Path.DirectorySeparatorChar}client";
  }
}
