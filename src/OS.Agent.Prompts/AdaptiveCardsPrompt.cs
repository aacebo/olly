using System.Text.Json;

using Microsoft.Teams.AI.Annotations;

using OS.Agent.Cards.Progress;

namespace OS.Agent.Prompts;

[Prompt]
[Prompt.Description("An agent that specializes in building rich UI experiences for Microsoft Teams using [Adaptive Cards](https://aacebo.ngrok.io/static/adaptive-cards.json)")]
[Prompt.Instructions(
    "An agent that specializes in building rich UI experiences for Microsoft Teams using [Adaptive Cards](https://aacebo.ngrok.io/static/adaptive-cards.json)",
    "The Adaptive Cards JSON Schema is available at `https://aacebo.ngrok.io/static/adaptive-cards.json`.",
    "!!You should always validate your cards against the provided JSON Schema!!",
    "Make sure you build a responsive and modern designed card!",
    "The Following Are Requirements:",
    "- urls should always be either a clickable link or button",
    "- cards should make use of columns as needed for better layouts",
    "- cards should always have a title"
)]
public class AdaptiveCardsPrompt
{
    [Function]
    [Function.Description(
        "Gets an adaptive card that represents some progress state",
        "Supported progress styles are 'in-progress', 'success', 'warning', 'error'"
    )]
    public string GetProgressCard([Param] string style, [Param] string? title, [Param] string? message)
    {
        var progressStyle = new ProgressStyle(style);

        if (!(progressStyle.IsInProgress || progressStyle.IsSuccess || progressStyle.IsWarning || progressStyle.IsError))
        {
            throw new InvalidOperationException("invalid style, supported values are 'in-progress', 'success', 'warning', 'error'");
        }

        var card = new ProgressCard(progressStyle);

        if (title is not null)
        {
            card = card.AddHeader(title);
        }

        card = progressStyle.IsInProgress
            ? card.AddProgressBar().AddFooter(message)
            : card.AddProgressBar(100).AddFooter(message);

        return JsonSerializer.Serialize(card);
    }
}