using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Teams.Cards;

namespace OS.Agent.Cards.Progress;

public class ProgressRing : CardElement
{
    public static ProgressRing? Deserialize(string json)
    {
        return JsonSerializer.Deserialize<ProgressRing>(json);
    }

    [JsonPropertyName("type")]
    public string Type { get; } = "ProgressRing";

    [JsonPropertyName("size")]
    public Size? Size { get; set; }

    [JsonPropertyName("isVisible")]
    public bool? IsVisible { get; set; }

    public string Serialize()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }

    public ProgressRing WithSize(Size size)
    {
        Size = size;
        return this;
    }

    public ProgressRing WithIsVisible(bool isVisible)
    {
        IsVisible = isVisible;
        return this;
    }
}