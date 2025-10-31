using Microsoft.Teams.Cards;
using Microsoft.Teams.Common;

namespace Olly.Cards.Progress;

public class ProgressCard : AdaptiveCard
{
    public ProgressStyle ProgressStyle { get; }

    public ProgressCard(ProgressStyle? style = null) : base()
    {
        ProgressStyle = style ?? ProgressStyle.InProgress;
    }

    public ProgressCard AddHeader(string title)
    {
        Body ??= [];
        Body.Add(
            new TextBlock(title)
                .WithWrap(true)
                .WithSize(TextSize.Large)
                .WithColor(ProgressStyle.IsError ? ProgressStyle.Color : TextColor.Default)
                .WithWeight(TextWeight.Bolder)
        );

        return this;
    }

    public ProgressCard AddProgressBar(int? value = null, int? max = null)
    {
        var card = new ProgressBar().WithColor(ProgressStyle.Color);

        if (value is not null)
        {
            card = card.WithValue(value.Value);
        }

        if (max is not null)
        {
            card = card.WithMax(max.Value);
        }

        Body ??= [];
        Body.Add(card);
        return this;
    }

    public ProgressCard AddFooter(string? message = null)
    {
        Body ??= [];
        Body.Add(
            new ColumnSet().WithColumns(
                new Column(
                    new Icon(ProgressStyle.Icon)
                        .WithSize(IconSize.XxSmall)
                        .WithColor(ProgressStyle.Color)
                )
                .WithWidth(new Union<string, float>("auto"))
                .WithVerticalContentAlignment(VerticalAlignment.Center),
                new Column(
                    new TextBlock(message ?? ProgressStyle.Message)
                        .WithSpacing(Spacing.ExtraSmall)
                        .WithSize(TextSize.Small)
                        .WithColor(ProgressStyle.Color)
                )
                .WithWidth(new Union<string, float>("stretch"))
                .WithVerticalContentAlignment(VerticalAlignment.Center)
                .WithSpacing(Spacing.Small)
            )
            .WithSpacing(Spacing.ExtraSmall)
        );

        return this;
    }
}