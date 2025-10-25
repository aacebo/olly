using Microsoft.Teams.Cards;

namespace OS.Agent.Cards.Extensions;

public static class CardElementExtensions
{
    public static AdaptiveCard ToAdaptiveCard(this CardElement element)
    {
        return element is AdaptiveCard card ? card : new(element);
    }
}