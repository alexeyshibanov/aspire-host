namespace VirtoCommerce.AppHost.Extensions;

/// <summary>
/// Centralized configuration for AppHost resources.
/// Set via appsettings.json Parameters section or environment variables (Parameters__KeyName).
/// </summary>
public record AppHostOptions
{
    /// <summary>
    /// Relative path to platform source code root.
    /// </summary>
    public string PlatformSourcePath { get; init; } = "../../vc-platform";

    /// <summary>
    /// Relative path to compiled platform release.
    /// </summary>
    public string PlatformReleasePath { get; init; } = "../../vc-platform-release";

    /// <summary>
    /// Relative path to deploy repository.
    /// </summary>
    public string DeployRepoPath { get; init; } = "../../deploy";

    /// <summary>
    /// Relative path to frontend application.
    /// </summary>
    public string FrontendPath { get; init; } = "../../frontend";

    public bool UsePlatformFromSource { get; init; } = true;

    public bool IncludePgAdmin { get; init; } = true;

    public bool IncludeRedis { get; init; } = true;

    public bool IncludeRedisInsight { get; init; } = true;

    public bool IncludeFrontend { get; init; } = true;

    public string PostgresVersion { get; init; } = "16";

    public string PgAdminVersion { get; init; } = "9.10";

    public string ElasticsearchVersion { get; init; } = "8.19.7";

    public string RedisVersion { get; init; } = "8.4-alpine";

    public string RedisInsightVersion { get; init; } = "2.70";

    public int PostgresPort { get; init; } = 5432;

    public int PgAdminPort { get; init; } = 5050;

    public int ElasticsearchPort { get; init; } = 9200;

    public int KibanaPort { get; init; } = 5601;

    public int RedisPort { get; init; } = 6379;

    public int RedisInsightPort { get; init; } = 5540;

    public int PlatformPort { get; init; } = 8090;

    public string ElasticsearchLicense { get; init; } = "basic";

    public string ElasticsearchMemoryLimit { get; init; } = "2g";

    public string ElasticsearchHeapSize { get; init; } = "1g";

    /// <summary>
    /// AppHost directory path. Set during initialization.
    /// </summary>
    public string AppHostDirectory { get; private init; } = string.Empty;

    /// <summary>
    /// Resolved value indicating whether to use platform from source.
    /// </summary>
    public bool IsSourceMode { get; private init; }

    /// <summary>
    /// Gets absolute path to platform web project based on current mode (source or release).
    /// </summary>
    public string GetPlatformWebPath()
    {
        return IsSourceMode
            ? Path.GetFullPath(Path.Combine(AppHostDirectory, PlatformSourcePath, "src/VirtoCommerce.Platform.Web"))
            : Path.GetFullPath(Path.Combine(AppHostDirectory, PlatformReleasePath));
    }

    /// <summary>
    /// Gets absolute path to platform .csproj file (for source mode only).
    /// </summary>
    public string GetPlatformProjectPath()
    {
        return Path.GetFullPath(Path.Combine(AppHostDirectory, PlatformSourcePath, "src/VirtoCommerce.Platform.Web/VirtoCommerce.Platform.Web.csproj"));
    }

    /// <summary>
    /// Gets absolute path to deploy repository.
    /// </summary>
    public string GetDeployRepoPath()
    {
        return Path.GetFullPath(Path.Combine(AppHostDirectory, DeployRepoPath));
    }

    /// <summary>
    /// Gets absolute path to frontend application.
    /// </summary>
    public string GetFrontendPath()
    {
        return Path.GetFullPath(Path.Combine(AppHostDirectory, FrontendPath));
    }

    public static AppHostOptions Load(IDistributedApplicationBuilder builder, string[] args)
    {
        var section = builder.Configuration.GetSection("Parameters");
        var defaults = new AppHostOptions();
        var usePlatformFromSource = GetBool(nameof(UsePlatformFromSource), defaults.UsePlatformFromSource);

        return new AppHostOptions
        {
            AppHostDirectory = builder.AppHostDirectory,
            IsSourceMode = usePlatformFromSource && !args.Contains("--no-source", StringComparer.OrdinalIgnoreCase),
            PlatformReleasePath = section[nameof(PlatformReleasePath)] ?? defaults.PlatformReleasePath,
            PlatformSourcePath = section[nameof(PlatformSourcePath)] ?? defaults.PlatformSourcePath,
            DeployRepoPath = section[nameof(DeployRepoPath)] ?? defaults.DeployRepoPath,
            FrontendPath = section[nameof(FrontendPath)] ?? defaults.FrontendPath,
            UsePlatformFromSource = usePlatformFromSource,
            IncludePgAdmin = GetBool(nameof(IncludePgAdmin), defaults.IncludePgAdmin),
            IncludeRedis = GetBool(nameof(IncludeRedis), defaults.IncludeRedis),
            IncludeRedisInsight = GetBool(nameof(IncludeRedisInsight), defaults.IncludeRedisInsight),
            IncludeFrontend = GetBool(nameof(IncludeFrontend), defaults.IncludeFrontend),
            PostgresVersion = section[nameof(PostgresVersion)] ?? defaults.PostgresVersion,
            PgAdminVersion = section[nameof(PgAdminVersion)] ?? defaults.PgAdminVersion,
            ElasticsearchVersion = section[nameof(ElasticsearchVersion)] ?? defaults.ElasticsearchVersion,
            RedisVersion = section[nameof(RedisVersion)] ?? defaults.RedisVersion,
            RedisInsightVersion = section[nameof(RedisInsightVersion)] ?? defaults.RedisInsightVersion,
            PostgresPort = GetInt(nameof(PostgresPort), defaults.PostgresPort),
            PgAdminPort = GetInt(nameof(PgAdminPort), defaults.PgAdminPort),
            ElasticsearchPort = GetInt(nameof(ElasticsearchPort), defaults.ElasticsearchPort),
            KibanaPort = GetInt(nameof(KibanaPort), defaults.KibanaPort),
            RedisPort = GetInt(nameof(RedisPort), defaults.RedisPort),
            RedisInsightPort = GetInt(nameof(RedisInsightPort), defaults.RedisInsightPort),
            PlatformPort = GetInt(nameof(PlatformPort), defaults.PlatformPort),
            ElasticsearchLicense = section[nameof(ElasticsearchLicense)] ?? defaults.ElasticsearchLicense,
            ElasticsearchMemoryLimit = section[nameof(ElasticsearchMemoryLimit)] ?? defaults.ElasticsearchMemoryLimit,
            ElasticsearchHeapSize = section[nameof(ElasticsearchHeapSize)] ?? defaults.ElasticsearchHeapSize,
        };

        int GetInt(string key, int defaultValue) =>
            int.TryParse(section[key], out var result) ? result : defaultValue;

        bool GetBool(string key, bool defaultValue) =>
            bool.TryParse(section[key], out var result) ? result : defaultValue;
    }
}
