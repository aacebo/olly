using System.Text.Json;

using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Cards;
using Microsoft.Teams.Common;

using Olly.Cards.Extensions;
using Olly.Cards.Progress;
using Olly.Errors;
using Olly.Storage;

namespace Olly.Api.Controllers.Teams.Dialogs;

public partial class DialogController
{
    protected async Task<AdaptiveCard> OnChatJobsFetch(Cards.Dialogs.OpenDialogRequest request, Tasks.FetchActivity activity, CancellationToken cancellationToken = default)
    {
        var chat = await Services.Chats.GetById(request.Get<Guid>("chat_id"), cancellationToken) ?? throw HttpException.NotFound().AddMessage("chat not found");

        return new AdaptiveCard(
            new CodeBlock()
                .WithLanguage(CodeLanguage.Json)
                .WithCodeSnippet(JsonSerializer.Serialize(chat, new JsonSerializerOptions()
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                }))
        );
    }

    protected async Task<AdaptiveCard> OnMessageJobsFetch(Cards.Dialogs.OpenDialogRequest request, Tasks.FetchActivity activity, CancellationToken cancellationToken = default)
    {
        var message = await Services.Messages.GetById(request.Get<Guid>("message_id"), cancellationToken) ?? throw HttpException.NotFound().AddMessage("message not found");
        var account = message.AccountId is null ? null : await Services.Accounts.GetById(message.AccountId.Value, cancellationToken);
        var jobs = await Services.Jobs.GetByMessageId(
            message.Id,
            Page.Create()
                .Sort(SortDirection.Desc, "created_at")
                .Size(50)
                .Build(),
            cancellationToken
        );

        return new AdaptiveCard(
            new Container([
                new Container(
                    new ColumnSet().WithColumns(
                        new Column(
                        )
                        .WithWidth(new Union<string, float>("auto")),
                        new Column(
                            new TextBlock($"__{account?.Name}__")
                                .WithIsSubtle(true)
                                .WithWeight(TextWeight.Bolder)
                                .WithSize(TextSize.Small),
                            new TextBlock($"_{message.Text}_")
                                .WithIsSubtle(true)
                                .WithSize(TextSize.Small)
                        )
                        .WithWidth(new Union<string, float>("stretch"))
                    )
                )
                .WithRoundedCorners(true)
                .WithStyle(ContainerStyle.Emphasis),
                ..jobs.List.Select(job =>
                {
                    var run = job.LastRunId is null
                        ? null
                        : Services.Runs.GetById(job.LastRunId.Value, cancellationToken: cancellationToken).GetAwaiter().GetResult();

                    var status = run is null ? ProgressStyle.InProgress : run.Status.ToProgressStyle();
                    var elapse = run?.EndedAt is null
                        ? null
                        : run.EndedAt - run.StartedAt;

                    return new ColumnSet().WithColumns(
                        new Column(
                            new Icon(status.Icon)
                                .WithColor(status.Color)
                                .WithSize(IconSize.XSmall)
                                .WithIsVisible(!status.IsInProgress),
                            new ProgressRing()
                                .WithSize(new("Tiny"))
                                .WithIsVisible(status.IsInProgress)
                        )
                        .WithWidth(new Union<string, float>("auto"))
                        .WithVerticalContentAlignment(VerticalAlignment.Center),
                        new Column(
                            new Container(
                                new TextBlock(job.Description ?? status.Message)
                                    .WithColor(status.Color)
                                    .WithSize(TextSize.Small)
                                    .WithSpacing(Spacing.Small)
                                    .WithIsVisible(job.Description is not null)
                                    .WithIsSubtle(true)
                                    .WithWrap(false),
                                new TextBlock($"__{run?.StatusMessage ?? status.Message}__")
                                    .WithColor(status.Color)
                                    .WithSize(TextSize.Small)
                                    .WithSpacing(Spacing.Small)
                                    .WithIsVisible(run?.StatusMessage is not null)
                                    .WithIsSubtle(true)
                                    .WithWrap(false)
                                    .WithWeight(TextWeight.Bolder)
                            )
                        )
                        .WithWidth(new Union<string, float>("stretch"))
                        .WithVerticalContentAlignment(VerticalAlignment.Center),
                        new Column(
                            new TextBlock($"{elapse?.Seconds}s")
                                .WithColor(status.Color)
                                .WithIsSubtle(true)
                                .WithHorizontalAlignment(HorizontalAlignment.Right)
                        )
                        .WithIsVisible(elapse is not null)
                        .WithWidth(new Union<string, float>("auto"))
                        .WithVerticalContentAlignment(VerticalAlignment.Center)
                    )
                    .WithRoundedCorners(true)
                    .WithShowBorder(true)
                    .WithStyle(status.ContainerStyle);
                }).ToList<CardElement>()
            ])
        );
    }
}