using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TowerFight.API.Health;

public class ApiHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) => 
        await Task.FromResult(HealthCheckResult.Healthy());
}