using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Skolio.Administration.Api.Diagnostics;

public sealed class RedisHealthCheck(IConnectionMultiplexer redis) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!redis.IsConnected)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Redis is disconnected."));
        }

        return Task.FromResult(HealthCheckResult.Healthy());
    }
}
