namespace OS.Agent.Api.Schema;

[GraphQLName("Source")]
public class SourceSchema
{
    [GraphQLName("id")]
    public string Id { get; set; }

    [GraphQLName("type")]
    public string Type { get; set; }

    [GraphQLName("url")]
    public string? Url { get; set; }

    public SourceSchema(Storage.Models.Source source)
    {
        Id = source.Id;
        Type = source.Type;
        Url = source.Url;
    }

    public SourceSchema(string id, string type, string? url = null)
    {
        Id = id;
        Type = type;
        Url = url;
    }
}