namespace VirtoCommerce.AppHost.Extensions;

public static class RedisExtensions
{
    public static IResourceBuilder<RedisResource>? AddRedisWithInsight(this IDistributedApplicationBuilder builder, AppHostOptions options)
    {
        if (!options.IncludeRedis)
        {
            return null;
        }

        var redisPassword = builder.AddParameter("RedisPassword", secret: true);
        var redis = builder.AddRedis("redis", password: redisPassword, port: options.RedisPort)
            .WithImageTag(options.RedisVersion)
            .WithDataVolume()
            .WithPersistence();

        if (options.IncludeRedisInsight)
        {
            redis.WithRedisInsight(insight => insight
                .WithImageTag(options.RedisInsightVersion)
                .WithHostPort(options.RedisInsightPort)
                .WithDataVolume());
        }

        redisPassword.WithParentRelationship(redis);

        return redis;
    }
}
