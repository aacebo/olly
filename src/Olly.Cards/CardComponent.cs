using Microsoft.Teams.Cards;

namespace Olly.Cards;

public abstract class CardComponent
{
    public abstract CardElement Render();
}

public abstract class CardComponent<TProps> where TProps : notnull
{
    public abstract CardElement Render(TProps props);
}