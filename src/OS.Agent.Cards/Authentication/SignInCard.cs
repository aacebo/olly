using Microsoft.Teams.Cards;

namespace OS.Agent.Cards.Authentication;

public class SignInCard : AdaptiveCard
{
    public SignInCard(string redirectUrl, string imageUrl) : base()
    {
        Body = [
            new Image(imageUrl)
                .WithHorizontalAlignment(HorizontalAlignment.Center)
                .WithStyle(ImageStyle.RoundedCorners)
                .WithSize(Size.Large)
        ];

        Actions = [
            new OpenUrlAction(redirectUrl)
                .WithTitle("Login")
                .WithStyle(ActionStyle.Positive)
                .WithIconUrl("icon:ShieldLock")
        ];
    }

    public static SignInCard Github(string redirectUrl) => new(redirectUrl, "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcR4ExGUTEwAQn95uM4KUU-OZ7Zz1n2lDrnXfw&s");
}