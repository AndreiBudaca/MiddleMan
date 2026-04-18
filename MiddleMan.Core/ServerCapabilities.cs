namespace MiddleMan.Core
{
  public static class ServerCapabilities
  {
    public static readonly int MaxChunkSize = int.TryParse(Environment.GetEnvironmentVariable("MIDDLEMAN_MAX_CHUNK_SIZE"), out int maxChunkSize) ? maxChunkSize : 32 * 1024; // 32KB

    public static readonly int IntraServerBufferedChunks = int.TryParse(Environment.GetEnvironmentVariable("MIDDLEMAN_INTRA_SERVER_BUFFERED_CHUNKS"), out int intraServerBufferedChunks) ? intraServerBufferedChunks : 10;

    public static readonly int GlobalTimeoutSeconds = int.TryParse(Environment.GetEnvironmentVariable("MIDDLEMAN_GLOBAL_TIMEOUT_SECONDS"), out int globalTimeoutSeconds) ? globalTimeoutSeconds : 10;

    public static readonly int ClientConnectionTimeoutSeconds = int.TryParse(Environment.GetEnvironmentVariable("MIDDLEMAN_CLIENT_CONNECTION_TIMEOUT_SECONDS"), out int clientConnectionTimeoutSeconds) ? clientConnectionTimeoutSeconds : 5;

    public static readonly bool ClusterMode = Environment.GetEnvironmentVariable("MIDDLEMAN_CLUSTER_MODE") == "true";

    public static readonly bool FaultToleranceEnabled = Environment.GetEnvironmentVariable("MIDDLEMAN_FAULT_TOLERANCE_ENABLED") == "true";

    public static readonly int MaxRetryAttempts = int.TryParse(Environment.GetEnvironmentVariable("MIDDLEMAN_MAX_RETRY_ATTEMPTS"), out int maxRetryAttempts) ? maxRetryAttempts : 3;

    public static readonly int[] AllowedVersions = [0];

    public static readonly string StaticFilesPath = Environment.GetEnvironmentVariable("MIDDLEMAN_LOCAL_BLOB_PATH") ?? $"{Directory.GetCurrentDirectory()}/blobs";

    public static readonly string UIStaticFilesPath = $"{Directory.GetCurrentDirectory()}/ClientApp/build/client";
  }
}
