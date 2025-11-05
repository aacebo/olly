using Microsoft.Teams.Cards;
using Microsoft.Teams.Common;

using Olly.Cards.Extensions;
using Olly.Storage.Models;

namespace Olly.Cards.Jobs;

public class JobRequest : CardComponent
{
    public static Builder New(SourceType type) => new(type);

    public required SourceType Type { get; set; }
    public required string Title { get; set; }
    public CardElement[] Body { get; set; } = [];

    public override CardElement Render()
    {
        return new Container(
            new ColumnSet().WithColumns(
                new Column(
                    new Container(
                        new Image(Type.GetImageUrl())
                            .WithSize(Size.Small)
                    )
                    .WithRoundedCorners(true)
                    .WithHorizontalAlignment(HorizontalAlignment.Center)
                    .WithVerticalContentAlignment(VerticalAlignment.Center)
                )
                .WithWidth(new Union<string, float>("auto"))
                .WithVerticalContentAlignment(VerticalAlignment.Center),
                new Column(
                    new TextBlock(Title)
                        .WithSize(TextSize.Large)
                        .WithWeight(TextWeight.Bolder)
                )
                .WithWidth(new Union<string, float>("stretch"))
                .WithVerticalContentAlignment(VerticalAlignment.Center)
            ),
            new Container(Body),
            new ActionSet(
                new SubmitAction()
                    .WithStyle(ActionStyle.Positive)
                    .WithTitle("Approve"),
                new SubmitAction()
                    .WithTitle("Reject")
            )
        );
    }

    public class Builder(SourceType type)
    {
        private string? _title;
        private CardElement[] _body = [];

        public Builder Title(string title)
        {
            _title = title;
            return this;
        }

        public Builder Body(params CardElement[] body)
        {
            _body = body;
            return this;
        }

        public JobRequest Build()
        {
            if (_title is null) throw new Exception("Title is required");

            return new()
            {
                Type = type,
                Title = _title,
                Body = _body
            };
        }
    }
}