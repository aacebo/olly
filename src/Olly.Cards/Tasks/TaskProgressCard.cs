using System.Text.Json.Serialization;

using Microsoft.Teams.Cards;
using Microsoft.Teams.Common;

using Olly.Cards.Dialogs;
using Olly.Cards.Progress;

namespace Olly.Cards.Tasks;

public class TaskProgressCardProps
{
    [JsonPropertyName("chat_id")]
    public required Guid ChatId { get; set; }

    [JsonPropertyName("message_id")]
    public Guid? MessageId { get; set; }
}

public class TaskProgressCard : CardComponent<TaskProgressCardProps>
{
    public TaskItem? Current { get; set; }
    public IList<TaskItem> Tasks { get; set; } = [];

    public TaskItem Add(TaskItem.Create create)
    {
        create.Style.Validate();
        Current = new()
        {
            Id = create.Id,
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

    public override AdaptiveCard Render(TaskProgressCardProps props)
    {
        if (Current is null) throw new InvalidOperationException();

        var inProgressCount = Tasks.Count(t => t.Style.IsInProgress);
        var successCount = Tasks.Count(t => t.Style.IsSuccess);
        var errorCount = Tasks.Count(t => t.Style.IsError);
        var warningCount = Tasks.Count(t => t.Style.IsWarning);
        var style = errorCount > 0 ? ProgressStyle.Error :
            warningCount > 0 ? ProgressStyle.Warning :
            successCount > 0 ? ProgressStyle.Success :
            Current.Style;

        var card = new ProgressCard(style);
        card.Body ??= [];
        card.Style = style.ContainerStyle;

        if (Current.Title is not null)
        {
            card.Body.Add(
                new ColumnSet()
                    .WithColumns(
                        new Column(
                            new TextBlock(Current.Title)
                                .WithWrap(true)
                                .WithSize(TextSize.Large)
                                .WithColor(style.IsError ? style.Color : TextColor.Default)
                                .WithWeight(TextWeight.Bolder)
                        )
                        .WithWidth(new Union<string, float>("stretch"))
                        .WithVerticalContentAlignment(VerticalAlignment.Center),
                        new Column(
                            new ColumnSet()
                                .WithColumns(
                                    new Column(
                                        new Badge()
                                            .WithAppearance(BadgeAppearance.Tint)
                                            .WithShape(BadgeShape.Rounded)
                                            .WithSize(BadgeSize.Large)
                                            .WithStyle(BadgeStyle.Attention)
                                            .WithText($"💀{errorCount}")
                                            .WithIsVisible(errorCount > 0)
                                    )
                                    .WithVerticalContentAlignment(VerticalAlignment.Center),
                                    new Column(
                                        new Badge()
                                            .WithAppearance(BadgeAppearance.Tint)
                                            .WithShape(BadgeShape.Rounded)
                                            .WithSize(BadgeSize.Large)
                                            .WithStyle(BadgeStyle.Warning)
                                            .WithText($"⚠️ {warningCount}")
                                            .WithIsVisible(warningCount > 0)
                                    )
                                    .WithVerticalContentAlignment(VerticalAlignment.Center)
                                )
                        )
                        .WithWidth(new Union<string, float>("auto"))
                        .WithVerticalContentAlignment(VerticalAlignment.Center)
                    )
            );
        }

        card = card
            .AddProgressBar(Tasks.Count - inProgressCount, Tasks.Count)
            .AddFooter(Current.Message);

        card.Body?.Add(
            new ActionSet(
                new TaskFetchAction(
                    new OpenDialogRequest(props.MessageId is null ? "chat.jobs" : "message.jobs", "Jobs")
                        .WithProperty("chat_id", props.ChatId)
                        .WithProperty("message_id", props.MessageId)
                        .ToDictionary()
                )
                .WithTitle($"View {Tasks.Count}")
            )
            .WithHorizontalAlignment(HorizontalAlignment.Right)
        );

        return card;
    }
}