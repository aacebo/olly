using Microsoft.Teams.Cards;
using Microsoft.Teams.Common;

using OS.Agent.Cards.Progress;

namespace OS.Agent.Cards.Tasks;

public class TaskProgressCard
{
    public TaskItem? Current { get; set; }
    public IList<TaskItem> Tasks { get; set; } = [];

    public TaskItem Add(TaskItem.Create create)
    {
        create.Style.Validate();
        Current = new()
        {
            Style = create.Style,
            Title = create.Title,
            Message = create.Message
        };

        Tasks = Tasks.Prepend(Current).ToList();
        return Current;
    }

    public TaskItem Update(Guid id, TaskItem.Update update)
    {
        update.Style?.Validate();
        var i = Tasks.ToList().FindIndex(t => t.Id == id);

        if (i == -1)
        {
            throw new InvalidOperationException("Task not found");
        }

        Tasks[i].Apply(update);

        if (Current is not null && Current.Id == id)
        {
            Current.Apply(update);
        }

        return Tasks[i];
    }

    public AdaptiveCard Build()
    {
        if (Current is null) throw new InvalidOperationException();

        var card = new ProgressCard(Current.Style);

        if (Current.Title is not null)
        {
            card = card.AddHeader(Current.Title);
        }

        card = card
            .AddProgressBar(Current.Style.IsInProgress ? null : 100)
            .AddFooter(Current.Message);

        card = (ProgressCard)card.WithStyle(Current.Style.ContainerStyle) ?? throw new InvalidDataException();
        card.Body?.Add(
            new ActionSet(
                new ShowCardAction()
                    .WithTitle($"Show {Tasks.Count}")
                    .WithTooltip("Task Status Updates")
                    .WithCard(new AdaptiveCard(
                        Tasks.Select(task =>
                        {
                            List<Column> columns = [
                                new Column(
                                    new Icon(task.Style.Icon)
                                        .WithColor(task.Style.Color)
                                        .WithSize(IconSize.XxSmall)
                                )
                                .WithWidth(new Union<string, float>("auto"))
                                .WithVerticalContentAlignment(VerticalAlignment.Center),
                                new Column(
                                    new TextBlock(task.Message)
                                        .WithColor(task.Style.Color)
                                        .WithSize(TextSize.Small)
                                        .WithSpacing(Spacing.Small)
                                        .WithIsSubtle(true)
                                        .WithWrap(false)
                                )
                                .WithWidth(new Union<string, float>("stetch"))
                                .WithVerticalContentAlignment(VerticalAlignment.Center)
                            ];

                            if (task.EndedAt is not null)
                            {
                                var elapse = task.EndedAt - task.StartedAt;

                                columns.Add(new Column(
                                    new TextBlock($"{elapse?.Seconds}s")
                                        .WithColor(task.Style.Color)
                                        .WithIsSubtle(true)
                                ));
                            }

                            return new ColumnSet()
                                .WithColumns(columns)
                                .WithShowBorder(true)
                                .WithRoundedCorners(true);
                        })
                        .ToList<CardElement>()
                    ))
            )
            .WithHorizontalAlignment(HorizontalAlignment.Right)
        );

        return card;
    }
}