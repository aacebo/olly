namespace OS.Agent.Api.Schema;

[GraphQLName("Source")]
public class SourceSchema(Storage.Models.Source source)
{
    [GraphQLName("id")]
    public string Id { get; set; } = source.Id;

    [GraphQLName("type")]
    public string Type { get; set; } = source.Type;
}