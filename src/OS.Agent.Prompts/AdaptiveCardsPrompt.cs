using Microsoft.Teams.AI.Annotations;
using Microsoft.Teams.Cards;

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
public class AdaptiveCardsPrompt(IPromptContext context)
{
    [Function]
    [Function.Description("Adds an adaptive card as an attachment to the response message")]
    public string AddAdaptiveCardToResponse([Param] string adaptiveCard)
    {
        var card = AdaptiveCard.Deserialize(adaptiveCard) ?? throw new InvalidDataException("Invalid Adaptive Card Payload");
        context.AddAdaptiveCard(card);
        return "<adaptive card added to response>";
    }
}