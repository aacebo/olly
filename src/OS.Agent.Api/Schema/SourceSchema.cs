namespace OS.Agent.Api.Schema;

[GraphQLName("Source")]
public class SourceSchema
{
    [GraphQLName("id")]
    public string Id { get; set; }

    [GraphQLName("type")]
    public string Type { get; set; }

    public SourceSchema(Storage.Models.Source source)
    {
        Id = source.Id;
        Type = source.Type;
    }

    public SourceSchema(string id, string type)
    {
        Id = id;
        Type = type;
    }
}