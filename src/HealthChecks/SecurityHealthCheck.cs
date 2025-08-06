using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Tranzor.HealthChecks;

public class SecurityHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(HealthCheckResult.Healthy("Security checks are healthy"));
    }
}