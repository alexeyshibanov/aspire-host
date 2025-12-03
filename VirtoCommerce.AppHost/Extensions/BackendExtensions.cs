using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace VirtoCommerce.AppHost.Extensions;

public static class BackendExtensions
{
    public static IResourceBuilder<IResourceWithEndpoints> AddVirtoCommercePlatform(
        this IDistributedApplicationBuilder builder,
        PostgresDatabases databases,
        ElasticsearchResources elastic,
        IResourceBuilder<RedisResource>? redis,
        AppHostOptions options)
    {
        IResourceBuilder<IResourceWithEndpoints> backend;

        if (options.IsSourceMode)
        {
            var backendProject = builder.AddProject("backend", options.GetPlatformProjectPath(), opts =>
            {
                opts.ExcludeLaunchProfile = true;
                opts.ExcludeKestrelEndpoints = true;
            });

            backend = ConfigureBackend(backendProject, databases, redis, elastic, options);
        }
        else
        {
            var platformReleasePath = options.GetPlatformWebPath();
            var dllPath = Path.GetFullPath(Path.Combine(platformReleasePath, "VirtoCommerce.Platform.Web.dll"));

            if (!File.Exists(dllPath))
            {
                throw new FileNotFoundException(
                    $"VirtoCommerce.Platform.Web.dll not found at {dllPath}. " +
                    $"Check 'PlatformReleasePath' parameter in appsettings.json (currently: '{options.PlatformReleasePath}') " +
                    $"or set Parameters__PlatformReleasePath environment variable.");
            }

            var backendExecutable = builder.AddExecutable("backend", "dotnet", platformReleasePath, "VirtoCommerce.Platform.Web.dll");
            backend = ConfigureBackend(backendExecutable, databases, redis, elastic, options);
        }

        return backend;
    }

    private static IResourceBuilder<TResource> ConfigureBackend<TResource>(
        IResourceBuilder<TResource> backendBuilder,
        PostgresDatabases databases,
        IResourceBuilder<RedisResource>? redis,
        ElasticsearchResources elastic,
        AppHostOptions options)
        where TResource : IResourceWithEndpoints, IResourceWithEnvironment, IResourceWithWaitSupport
    {
        backendBuilder
            .WithHttpsEndpoint(options.PlatformPort)
            .WithHttpHealthCheck("/health")
            .WithEnvironment("DatabaseProvider", "PostgreSql")
            .WithReference(databases.Platform, "VirtoCommerce")
            .WithReference(databases.Hangfire, "VirtoCommerce.Hangfire")
            .WithReference(databases.Catalog, "VirtoCommerce.Catalog")
            .WithReference(databases.Cart, "VirtoCommerce.Cart")
            .WithReference(databases.Orders, "VirtoCommerce.Orders")
            .WithReference(databases.Orders, "VirtoCommerce.Quote")
            .WithReference(databases.Notifications, "VirtoCommerce.Notifications")
            .WithReference(databases.AuditLog, "VirtoCommerce.AuditLog")
            .WithEnvironment(context =>
            {
                var backendEndpoint = backendBuilder.GetEndpoint("https");
                context.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Development";
                context.EnvironmentVariables["VirtoCommerce__Hangfire__JobStorageType"] = "Database";
                context.EnvironmentVariables["VirtoCommerce__ModuleSequenceBoost__0"] = "LEO.AspireIntegration";
                context.EnvironmentVariables["VirtoCommerce__ModuleSequenceBoost__1"] = "VirtoCommerce.AuditLog";
                context.EnvironmentVariables["Assets__FileSystem__PublicUrl"] = $"{backendEndpoint.Url}/assets/";
                context.EnvironmentVariables["Content__FileSystem__PublicUrl"] = $"{backendEndpoint.Url}/cms-content/";
                context.EnvironmentVariables["Search__Provider"] = "ElasticSearch8";
                context.EnvironmentVariables["Search__ElasticSearch8__Server"] = elastic.Elasticsearch.GetEndpoint("https");
                context.EnvironmentVariables["Search__ElasticSearch8__User"] = "elastic";
                context.EnvironmentVariables["Search__ElasticSearch8__Key"] = elastic.ElasticPassword.Resource;
                context.EnvironmentVariables["Search__ElasticSearch8__CertificateFingerprint"] = elastic.CertFingerprint;
                context.EnvironmentVariables["Search__ContentFullTextSearchEnabled"] = "true";
                context.EnvironmentVariables["Search__OrderFullTextSearchEnabled"] = "true";
                context.EnvironmentVariables["AuditLogOptions__CanConfigureTrackingEvents"] = "true";
                context.EnvironmentVariables["Serilog__MinimumLevel__Default"] = "Warning";
                context.EnvironmentVariables["Serilog__MinimumLevel__Override__System"] = "Warning";
                context.EnvironmentVariables["Serilog__MinimumLevel__Override__Microsoft"] = "Warning";
                context.EnvironmentVariables["Serilog__MinimumLevel__Override__Microsoft.Hosting.Lifetime"] = "Information";
                context.EnvironmentVariables["Serilog__MinimumLevel__Override__Microsoft.AspNetCore.SignalR"] = "Warning";
                context.EnvironmentVariables["Serilog__MinimumLevel__Override__Microsoft.AspNetCore.Http.Connections"] = "Warning";
                context.EnvironmentVariables["Serilog__MinimumLevel__Override__VirtoCommerce.Platform.Modules"] = "Information";
                context.EnvironmentVariables["Serilog__MinimumLevel__Override__VirtoCommerce.Platform.Web.Startup"] = "Information";
            })
            .WaitFor(databases.Server)
            .WaitFor(elastic.Elasticsearch);

        if (redis is not null)
        {
            backendBuilder
                .WithReference(redis, "RedisConnectionString")
                .WaitFor(redis);
        }

        backendBuilder.WithCommand(
            name: "update-vc-build",
            displayName: "Update vc-build",
            executeCommand: context => ExecuteCommandWithLoggingAsync(context, options, UpdateVcBuildCommandAsync),
            new CommandOptions
            {
                Description = "Updates the VirtoCommerce.GlobalTool to the latest version available on NuGet.",
                IconName = "ArrowSync",
                IconVariant = IconVariant.Filled,
            });

        backendBuilder.WithCommand(
            name: "update-modules",
            displayName: "Update modules",
            executeCommand: context => ExecuteCommandWithLoggingAsync(context, options, UpdateModulesCommandAsync),
            new CommandOptions
            {
                Description = "Fetches latest packages manifest and updates modules",
                IconName = "ArrowDownload",
                IconVariant = IconVariant.Filled,
            });

        return backendBuilder;
    }

    private static async Task<ExecuteCommandResult> UpdateModulesCommandAsync(ExecuteCommandContext context, ILogger logger, AppHostOptions options)
    {
        var deployRepoPath = options.GetDeployRepoPath();
        var platformWebPath = options.GetPlatformWebPath();

        logger.LogInformation("Starting module update process...");
        logger.LogInformation("Using platform from: {Mode}", options.IsSourceMode ? "Source" : "Release");
        logger.LogInformation("Deploy Repo Path: {Path}", deployRepoPath);
        logger.LogInformation("Platform Web Path: {Path}", platformWebPath);

        logger.LogInformation("Fetching latest changes from deploy repository...");

        if (!Directory.Exists(deployRepoPath))
        {
            logger.LogError("Deploy repository not found at {Path}", deployRepoPath);

            return CommandResults.Failure($"Deploy repository not found at {deployRepoPath}");
        }

        await ExecuteProcessAsync(logger, "git", "fetch origin main", deployRepoPath, cancellationToken: context.CancellationToken);

        var output = await ExecuteProcessAsync(logger, "git", "--no-pager show origin/main:backend/packages.DEV.json", deployRepoPath, captureOutput: true,
            context.CancellationToken);

        logger.LogInformation("Processing packages manifest...");

        // We need to remove "AzureUniversalPackages" source
        var jsonNode = JsonNode.Parse(output ?? "{}");
        if (jsonNode?["Sources"] is not JsonArray sources)
        {
            logger.LogError("Failed to parse packages.DEV.json content");

            return CommandResults.Failure("Failed to parse packages.DEV.json content");
        }

        var azureSource = sources.FirstOrDefault(node => node?["Name"]?.GetValue<string>() == "AzureUniversalPackages");
        if (azureSource is not null)
        {
            sources.Remove(azureSource);
            logger.LogInformation("Removed 'AzureUniversalPackages' source from manifest");
        }

        var tempFileName = $"vc-packages-{Guid.NewGuid()}.json";
        var tempFilePath = Path.Combine(options.AppHostDirectory, tempFileName);

        try
        {
            await File.WriteAllTextAsync(tempFilePath, jsonNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), context.CancellationToken);
            logger.LogInformation("Saved modified manifest to {Path}", tempFilePath);

            logger.LogInformation("Updating modules...");

            if (!Directory.Exists(platformWebPath))
            {
                logger.LogError("Platform Web directory not found at {Path}", platformWebPath);

                return CommandResults.Failure($"Platform Web directory not found at {platformWebPath}");
            }

            await ExecuteProcessAsync(logger, "vc-build", $"InstallModules --package-manifest-path \"{tempFilePath}\" --skip-dependency-solving",
                platformWebPath, cancellationToken: context.CancellationToken);
        }
        finally
        {
            try
            {
                File.Delete(tempFilePath);
                logger.LogInformation("Deleted temp file {Path}", tempFilePath);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete temp file {Path}", tempFilePath);
            }
        }

        logger.LogInformation("Successfully updated modules");

        return CommandResults.Success();
    }

    private static async Task<ExecuteCommandResult> UpdateVcBuildCommandAsync(
        ExecuteCommandContext context,
        ILogger logger,
        AppHostOptions options)
    {
        logger.LogInformation("Updating VirtoCommerce.GlobalTool...");

        await ExecuteProcessAsync(logger, "dotnet", "tool update --global VirtoCommerce.GlobalTool", cancellationToken: context.CancellationToken);

        logger.LogInformation("Successfully updated VirtoCommerce.GlobalTool");

        return CommandResults.Success();
    }

    private static async Task<ExecuteCommandResult> ExecuteCommandWithLoggingAsync(
        ExecuteCommandContext context,
        AppHostOptions options,
        Func<ExecuteCommandContext, ILogger, AppHostOptions, Task<ExecuteCommandResult>> action)
    {
        var loggerService = context.ServiceProvider.GetRequiredService<ResourceLoggerService>();
        var logger = loggerService.GetLogger(context.ResourceName);

        try
        {
            return await action(context, logger, options);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Operation was cancelled");

            return CommandResults.Failure("Operation cancelled");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while executing the command");

            return CommandResults.Failure(ex.Message);
        }
    }

    private static async Task<string?> ExecuteProcessAsync(
        ILogger logger,
        string fileName,
        string arguments,
        string? workingDirectory = null,
        bool captureOutput = false,
        CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory,
        };

        var output = captureOutput ? new StringBuilder() : null;

        using var process = new Process();
        process.StartInfo = startInfo;

        process.OutputDataReceived += (_, args) =>
        {
            if (string.IsNullOrEmpty(args.Data))
            {
                return;
            }

            if (output is not null)
            {
                output.AppendLine(args.Data);
            }
            else
            {
                logger.LogInformation("{Output}", args.Data);
            }
        };

        process.ErrorDataReceived += (_, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                logger.LogError("{Error}", args.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Failed to kill process during cleanup.");
            }

            throw;
        }

        return process.ExitCode == 0
            ? output?.ToString()
            : throw new InvalidOperationException($"Command '{fileName} {arguments}' failed with exit code {process.ExitCode}");
    }
}
