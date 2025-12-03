namespace VirtoCommerce.AppHost.Extensions;

public static class FrontendExtensions
{
    public static IResourceBuilder<NodeAppResource>? AddFrontend(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithEndpoints> backend,
        AppHostOptions options)
    {
        if (!options.IncludeFrontend)
        {
            return null;
        }

        var frontendPath = options.GetFrontendPath();

        return builder.AddNpmApp("frontend", frontendPath, "dev")
            .WithHttpsEndpoint(port: 3000, targetPort: 3000, name: "https", isProxied: false)
            .WithExternalHttpEndpoints()
            .WithEnvironment("APP_BACKEND_URL", backend.GetEndpoint("https"))
            .WaitFor(backend);
    }
}
