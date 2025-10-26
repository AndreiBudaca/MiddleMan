namespace MiddleMan.Core
{
  public static class ServerCapabilities
  {
    public const int MaxContentLength = 4 * 1024; // 4KB

    public static readonly int[] AllowedVersions = [0];
  }
}
