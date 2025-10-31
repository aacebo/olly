using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Teams.Cards;

namespace Olly.Cards.Progress;

public class ProgressBar : CardElement
{
    public static ProgressBar? Deserialize(string json)
    {
        return JsonSerializer.Deserialize<ProgressBar>(json);
    }

    [JsonPropertyName("type")]
    public string Type { get; } = "ProgressBar";

    [JsonPropertyName("color")]
    public TextColor? Color { get; set; }

    [JsonPropertyName("value")]
    public int? Value { get; set; }

    [JsonPropertyName("max")]
    public int? Max { get; set; }

    public string Serialize()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }

    public ProgressBar WithColor(TextColor color)
    {
        Color = color;
        return this;
    }

    public ProgressBar WithValue(int value)
    {
        Value = value;
        return this;
    }

    public ProgressBar WithMax(int max)
    {
        Max = max;
        return this;
    }
}