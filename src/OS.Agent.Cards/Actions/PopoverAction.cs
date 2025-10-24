using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Teams.Cards;

namespace OS.Agent.Cards.Actions;

public class PopoverAction(AdaptiveCard content) : Microsoft.Teams.Cards.Action
{
    public static PopoverAction? Deserialize(string json)
    {
        return JsonSerializer.Deserialize<PopoverAction>(json);
    }

    [JsonPropertyName("type")]
    public string Type { get; } = "Action.Popover";

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("tooltip")]
    public string? Tooltip { get; set; }

    [JsonPropertyName("maxPopoverWidth")]
    public string? MaxPopoverWidth { get; set; }

    [JsonPropertyName("content")]
    public AdaptiveCard Content { get; set; } = content;

    [JsonPropertyName("isEnabled")]
    public bool? IsEnabled { get; set; }

    public string Serialize()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }

    public PopoverAction WithTitle(string title)
    {
        Title = title;
        return this;
    }

    public PopoverAction WithTooltip(string tooltip)
    {
        Tooltip = tooltip;
        return this;
    }

    public PopoverAction WithMaxPopoverWidth(string maxPopoverWidth)
    {
        MaxPopoverWidth = maxPopoverWidth;
        return this;
    }

    public PopoverAction WithIsEnabled(bool enabled)
    {
        IsEnabled = enabled;
        return this;
    }
}