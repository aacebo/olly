using System.Diagnostics;

using Olly.Services;

namespace Olly.Api.Schema;

public class Query
{
    [GraphQLName("started_at")]
    public DateTime StartedAt => Process.GetCurrentProcess().StartTime;

    [GraphQLName("tenants")]
    public async Task<IEnumerable<TenantSchema>> GetTenants([Service] ITenantService tenantService, CancellationToken cancellationToken = default)
    {
        var res = await tenantService.Get(new(), cancellationToken);
        return res.List.Select(tenant => new TenantSchema(tenant));
    }

    [GraphQLName("tenant_by_id")]
    public async Task<TenantSchema?> GetTenantById([Service] ITenantService tenantService, Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = await tenantService.GetById(id, cancellationToken);
        return tenant is null ? null : new(tenant);
    }
}