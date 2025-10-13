using System.Text.Json.Serialization;

namespace OS.Agent.Models;

[Model]
public class Source : Model, IEquatable<Source>
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

    public override bool Equals(object? other) => Equals(other as Source);
    public bool Equals(Source? other) => other is not null && other.Type == Type && other.Id == Id;
    public override int GetHashCode() => base.GetHashCode();
    public static bool operator ==(Source a, Source b) => a.Equals(b);
    public static bool operator !=(Source a, Source b) => !a.Equals(b);
}

[JsonConverter(typeof(Converter<SourceType>))]
public class SourceType(string value) : StringEnum(value)
{
    public static readonly SourceType Github = new("github");
    public bool IsGithub => Github.Equals(Value);

    public static readonly SourceType Teams = new("teams");
    public bool IsTeams => Teams.Equals(Value);
}