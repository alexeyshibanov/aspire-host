namespace VirtoCommerce.AppHost.Extensions;

public static class PostgresExtensions
{
    public static PostgresDatabases AddPostgresWithDatabases(
        this IDistributedApplicationBuilder builder,
        AppHostOptions options)
    {
        var postgresPassword = builder.AddParameter("PostgresPassword", secret: true);
        var postgres = builder.AddPostgres("postgres", password: postgresPassword, port: options.PostgresPort)
            .WithImageTag(options.PostgresVersion)
            .WithDataVolume();

        if (options.IncludePgAdmin)
        {
            postgres.WithPgAdmin(pgAdmin => pgAdmin
                .WithImageTag(options.PgAdminVersion)
                .WithHostPort(options.PgAdminPort)
                .WithVolume(VolumeNameGenerator.Generate(pgAdmin, "data"), "/var/lib/pgadmin"));
        }

        postgresPassword.WithParentRelationship(postgres);

        return new PostgresDatabases
        {
            Server = postgres,
            Platform = postgres.AddDatabase("platform"),
            Hangfire = postgres.AddDatabase("hangfire"),
            Catalog = postgres.AddDatabase("catalog"),
            Cart = postgres.AddDatabase("cart"),
            Orders = postgres.AddDatabase("orders"),
            Notifications = postgres.AddDatabase("notifications"),
            AuditLog = postgres.AddDatabase("auditlog"),
        };
    }
}

public record PostgresDatabases
{
    public required IResourceBuilder<PostgresServerResource> Server { get; init; }

    public required IResourceBuilder<PostgresDatabaseResource> Platform { get; init; }

    public required IResourceBuilder<PostgresDatabaseResource> Hangfire { get; init; }

    public required IResourceBuilder<PostgresDatabaseResource> Catalog { get; init; }

    public required IResourceBuilder<PostgresDatabaseResource> Cart { get; init; }

    public required IResourceBuilder<PostgresDatabaseResource> Orders { get; init; }

    public required IResourceBuilder<PostgresDatabaseResource> Notifications { get; init; }

    public required IResourceBuilder<PostgresDatabaseResource> AuditLog { get; init; }
}
