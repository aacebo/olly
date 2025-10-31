using Olly.Services;

namespace Olly.Api.Schema;

[GraphQLName("Record")]
public class RecordSchema(Storage.Models.Record record) : ModelSchema
{
    [GraphQLName("id")]
    public Guid Id { get; init; } = record.Id;

    [GraphQLName("source")]
    public SourceSchema Source { get; set; } = new(record.SourceId, record.SourceType, record.Url);

    [GraphQLName("type")]
    public string Type { get; set; } = record.Type;

    [GraphQLName("name")]
    public string? Name { get; set; } = record.Name;

    [GraphQLName("entities")]
    public IEnumerable<EntitySchema> Entities { get; set; } = record.Entities.Select(entity => new EntitySchema(entity));

    [GraphQLName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = record.CreatedAt;

    [GraphQLName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = record.UpdatedAt;

    [GraphQLName("parent")]
    public async Task<RecordSchema?> GetParent([Service] IRecordService recordService, CancellationToken cancellationToken = default)
    {
        if (record.ParentId is null) return null;
        var parent = await recordService.GetById(record.ParentId.Value, cancellationToken);
        return parent is null ? null : new(parent);
    }

    [GraphQLName("records")]
    public async Task<IEnumerable<RecordSchema>> GetRecords([Service] IRecordService recordService, CancellationToken cancellationToken = default)
    {
        var records = await recordService.GetByParentId(Id, cancellationToken);
        return records.Select(record => new RecordSchema(record));
    }
}