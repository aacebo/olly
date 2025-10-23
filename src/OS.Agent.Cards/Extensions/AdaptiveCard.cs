using Microsoft.Teams.Api;
using Microsoft.Teams.Cards;

namespace OS.Agent.Cards.Extensions;

public static class AdaptiveCardExtensions
{
    public static Attachment ToAttachment(this AdaptiveCard card)
    {
        return new Attachment(card);
    }
}