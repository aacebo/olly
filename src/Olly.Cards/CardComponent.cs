using Microsoft.Teams.Cards;

namespace Olly.Cards;

public abstract class CardComponent
{
    public abstract CardElement Render();
}