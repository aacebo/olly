using Microsoft.Teams.Api;
using Microsoft.Teams.Cards;

namespace Olly.Drivers.Teams.Extensions;

public static class AdaptiveCardExtensions
{
    public static Attachment ToAttachment(this AdaptiveCard card)
    {
        return new(card);
    }
}