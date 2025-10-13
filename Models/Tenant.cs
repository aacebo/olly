using System.Text.Json.Serialization;

using SqlKata;

namespace OS.Agent.Models;

[Model]
public class Tenant : Model<Data>
{
    [Column("id")]
    [JsonPropertyName("id")]
    public Guid Id { get; init; } = Guid.NewGuid();

    [Column("sources")]
    [JsonPropertyName("sources")]
    public SourceList Sources { get; set; } = [];

    [Column("name")]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [Column("created_at")]
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public class Source
    {
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        [JsonPropertyName("type")]
        public required SourceType Type { get; set; }

        public static Source Teams(string id) => new()
        {
            Type = SourceType.Teams,
            Id = id
        };

        public static Source Github(string id) => new()
        {
            Type = SourceType.Github,
            Id = id
        };
    }

    public class SourceList : List<Source>, IList<Source>
    {
        
    }
}