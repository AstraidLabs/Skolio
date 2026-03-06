using Hangfire;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Skolio.Administration.Api.Diagnostics;

public sealed class HangfireHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var monitoring = JobStorage.Current.GetMonitoringApi();
            _ = monitoring.Servers();
            return Task.FromResult(HealthCheckResult.Healthy());
        }
        catch (Exception exception)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Hangfire monitoring API unavailable.", exception));
        }
    }
}
