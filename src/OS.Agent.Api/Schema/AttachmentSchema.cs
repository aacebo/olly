namespace OS.Agent.Api.Schema;

[GraphQLName("Attachment")]
public class AttachmentSchema(Storage.Models.Attachment attachment)
{
    [GraphQLName("id")]
    public string? Id { get; set; } = attachment.Id;

    [GraphQLName("name")]
    public string? Name { get; set; } = attachment.Name;

    [GraphQLName("content_type")]
    public string ContentType { get; set; } = attachment.ContentType;

    [GraphQLName("content")]
    public string Content { get; set; } = attachment.Content;
}