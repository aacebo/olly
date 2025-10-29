using Microsoft.Teams.Cards;
using Microsoft.Teams.Common;

namespace OS.Agent.Cards.Authentication;

public class SignInCard : CardComponent
{
    public required string Title { get; set; }
    public required string RedirectUrl { get; set; }
    public required string ImageUrl { get; set; }

    public override CardElement Render()
    {
        return new AdaptiveCard(
            new ColumnSet()
                .WithColumns(
                    new Column(
                        new Image(ImageUrl)
                            .WithWidth("150px")
                            .WithHeight("150px")
                    )
                    .WithWidth(new Union<string, float>("auto"))
                    .WithHorizontalAlignment(HorizontalAlignment.Center)
                    .WithRoundedCorners(true)
                    .WithSpacing(Spacing.None),
                    new Column(
                        new Container(
                            new TextBlock(Title)
                                .WithHorizontalAlignment(HorizontalAlignment.Left)
                                .WithSpacing(Spacing.None)
                                .WithStyle(new("heading")),
                            new TextBlock("ℹ️ By connecting your GitHub account, I'll be able to access your GitHub data to better assist you ℹ️")
                                .WithWrap(true)
                                .WithHorizontalAlignment(HorizontalAlignment.Left)
                                .WithHeight(ElementHeight.Stretch)
                        )
                        .WithSpacing(Spacing.None)
                        .WithHeight(ElementHeight.Stretch),
                        new ActionSet(
                            new OpenUrlAction(RedirectUrl)
                                .WithTitle("Connect")
                                .WithStyle(ActionStyle.Positive)
                                .WithIconUrl("icon:ShieldLock")
                        )
                        .WithSpacing(Spacing.None)
                        .WithHorizontalAlignment(HorizontalAlignment.Right)
                    )
                    .WithWidth(new Union<string, float>("stretch"))
                    .WithVerticalContentAlignment(VerticalAlignment.Bottom)
                )
                .WithSpacing(Spacing.None)
        );
    }

    public static SignInCard Github(string redirectUrl) => new()
    {
        Title = "GitHub",
        RedirectUrl = redirectUrl,
        ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcR4ExGUTEwAQn95uM4KUU-OZ7Zz1n2lDrnXfw&s"
    };
}