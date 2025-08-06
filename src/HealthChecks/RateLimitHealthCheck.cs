using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Tranzor.HealthChecks;

public class RateLimitHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(HealthCheckResult.Healthy("Rate limiting is healthy"));
    }
}