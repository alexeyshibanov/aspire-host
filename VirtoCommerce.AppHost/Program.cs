using VirtoCommerce.AppHost.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

var options = AppHostOptions.Load(builder, args);

// Infrastructure
var databases = builder.AddPostgresWithDatabases(options);
var elastic = builder.AddElasticsearchWithKibana(options);
var redis = builder.AddRedisWithInsight(options);

// Applications
var backend = builder.AddVirtoCommercePlatform(databases, elastic, redis, options);
builder.AddFrontend(backend, options);

builder.Build().Run();
