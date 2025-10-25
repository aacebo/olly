using Microsoft.Teams.Cards;

namespace OS.Agent.Cards;

public abstract class CardComponent
{
    public abstract CardElement Render();
}