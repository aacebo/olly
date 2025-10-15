using Microsoft.Teams.Cards;

namespace OS.Agent.Cards;

public static partial class Auth
{
    public static AdaptiveCard SignIn(string url, string? imageUrl = null)
    {
        return new AdaptiveCard(
            new Image(imageUrl ?? "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcR4ExGUTEwAQn95uM4KUU-OZ7Zz1n2lDrnXfw&s")
                .WithHorizontalAlignment(HorizontalAlignment.Center)
                .WithStyle(ImageStyle.RoundedCorners)
                .WithSize(Size.Large),
            new ActionSet(
                new OpenUrlAction(url)
                    .WithTitle("Login")
                    .WithStyle(ActionStyle.Positive)
                    .WithIconUrl("icon:ShieldLock")
            ).WithHorizontalAlignment(HorizontalAlignment.Center)
        );
    }
}