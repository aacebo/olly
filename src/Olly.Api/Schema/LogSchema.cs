using Olly.Services;

namespace Olly.Api.Schema;

[GraphQLName("Log")]
public class LogSchema(Storage.Models.Log log)
{
    [GraphQLName("id")]
    public Guid Id { get; init; } = log.Id;

    [GraphQLName("level")]
    public string Level { get; set; } = log.Level;

    [GraphQLName("type")]
    public string Type { get; set; } = log.Type;

    [GraphQLName("text")]
    public string Text { get; set; } = log.Text;

    [GraphQLName("entities")]
    public IEnumerable<EntitySchema> Entities { get; set; } = log.Entities.Select(entity => new EntitySchema(entity));

    [GraphQLName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = log.CreatedAt;

    [GraphQLName("tenant")]
    public async Task<TenantSchema> GetTenant([Service] ITenantService tenantService, CancellationToken cancellationToken = default)
    {
        var tenant = await tenantService.GetById(log.TenantId, cancellationToken);

        if (tenant is null)
        {
            throw new InvalidDataException();
        }

        return new(tenant);
    }

    [GraphQLName("subject")]
    public async Task<ModelSchema?> GetSubject([Service] IServices services, CancellationToken cancellationToken = default)
    {
        if (log.TypeId is null) return null;
        if (log.Type == Storage.Models.LogType.Tenant)
        {
            var value = await services.Tenants.GetById(Guid.Parse(log.TypeId), cancellationToken);
            return value is null ? null : new TenantSchema(value);
        }
        else if (log.Type == Storage.Models.LogType.Account)
        {
            var value = await services.Accounts.GetById(Guid.Parse(log.TypeId), cancellationToken);
            return value is null ? null : new AccountSchema(value);
        }
        else if (log.Type == Storage.Models.LogType.Chat)
        {
            var value = await services.Chats.GetById(Guid.Parse(log.TypeId), cancellationToken);
            return value is null ? null : new ChatSchema(value);
        }
        else if (log.Type == Storage.Models.LogType.Message)
        {
            var value = await services.Messages.GetById(Guid.Parse(log.TypeId), cancellationToken);
            return value is null ? null : new MessageSchema(value);
        }
        else if (log.Type == Storage.Models.LogType.Install)
        {
            var value = await services.Installs.GetById(Guid.Parse(log.TypeId), cancellationToken);
            return value is null ? null : new InstallSchema(value);
        }
        else if (log.Type == Storage.Models.LogType.Job || log.Type == Storage.Models.LogType.JobApproval)
        {
            var value = await services.Jobs.GetById(Guid.Parse(log.TypeId), cancellationToken);
            return value is null ? null : new JobSchema(value);
        }
        else if (log.Type == Storage.Models.LogType.JobRun)
        {
            var value = await services.Runs.GetById(Guid.Parse(log.TypeId), cancellationToken);
            return value is null ? null : new JobRunSchema(value);
        }

        return null;
    }
}