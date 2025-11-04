using System.Text.Json;
using System.Text.Json.Serialization;

namespace Olly.Cards.Dialogs;

public class OpenDialogRequest(string id, string title)
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = id;

    [JsonPropertyName("title")]
    public string Title { get; set; } = title;

    [JsonPropertyName("data")]
    public IDictionary<string, object?> Data { get; set; } = new Dictionary<string, object?>();

    public OpenDialogRequest WithProperty(string key, object? value)
    {
        Data[key] = value;
        return this;
    }

    public OpenDialogRequest WithData(Dictionary<string, object?> data)
    {
        Data = data;
        return this;
    }

    public T Get<T>(string key)
    {
        if (!Data.TryGetValue(key, out var value) || value is null)
        {
            throw new Exception($"data.${key} not found");
        }

        if (value is JsonElement element)
        {
            return element.Deserialize<T>(JsonSerializerOptions.Web) ?? throw new JsonException();
        }

        return (T)value;
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