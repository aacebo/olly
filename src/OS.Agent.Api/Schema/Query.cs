using System.Diagnostics;

using OS.Agent.Services;

namespace OS.Agent.Api.Schema;

public class Query([Service] ITenantService tenantService)
{
    [GraphQLName("started_at")]
    public DateTime StartedAt => Process.GetCurrentProcess().StartTime;

    [GraphQLName("tenant_by_id")]
    public async Task<TenantSchema?> GetTenantById(Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = await tenantService.GetById(id, cancellationToken);
        return tenant is null ? null : new(tenant);
    }
}