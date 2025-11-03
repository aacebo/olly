using System.Text.Json.Serialization;

namespace Olly.Cards.Dialogs;

public class OpenDialogRequest(string id, string title)
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = id;

    [JsonPropertyName("title")]
    public string Title { get; set; } = title;

    [JsonPropertyName("data")]
    public object? Data { get; set; }

    public OpenDialogRequest WithData(object? data)
    {
        Data = data;
        return this;
    }

    public IDictionary<string, object?> ToDictionary()
    {
        return new Dictionary<string, object?>()
        {
            {"id", Id},
            {"title", Title},
            {"data", Data}
        };
    }
}