using Microsoft.Teams.Cards;

namespace OS.Agent.Cards.Authentication;

public class SignInCard : CardComponent
{
    public required string RedirectUrl { get; set; }
    public required string ImageUrl { get; set; }

    public override CardElement Render()
    {
        return new AdaptiveCard(
            new Image(ImageUrl)
                .WithHorizontalAlignment(HorizontalAlignment.Center)
                .WithStyle(ImageStyle.RoundedCorners)
                .WithSize(Size.Large)
        )
        .WithActions(
            new OpenUrlAction(RedirectUrl)
                .WithTitle("Login")
                .WithStyle(ActionStyle.Positive)
                .WithIconUrl("icon:ShieldLock")
        );
    }

    public static SignInCard Github(string redirectUrl) => new()
    {
        RedirectUrl = redirectUrl,
        ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcR4ExGUTEwAQn95uM4KUU-OZ7Zz1n2lDrnXfw&s"
    };
}