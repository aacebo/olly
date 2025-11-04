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
                            new ComUserMicrosoftGraphComponent()
                                .WithProperties(
                                    new PersonaProperties()
                                        .WithDisplayName(account?.Name ?? "??")
                                )
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
                    var elapse = job.EndedAt is null
                        ? null
                        : job.EndedAt - job.StartedAt;

                    return new ColumnSet().WithColumns(
                        new Column(
                            new Icon(job.Status.ToProgressStyle().Icon)
                                .WithColor(job.Status.ToProgressStyle().Color)
                                .WithSize(IconSize.XSmall)
                                .WithIsVisible(!job.Status.ToProgressStyle().IsInProgress),
                            new ProgressRing()
                                .WithSize(new("Tiny"))
                                .WithIsVisible(job.Status.ToProgressStyle().IsInProgress)
                        )
                        .WithWidth(new Union<string, float>("auto"))
                        .WithVerticalContentAlignment(VerticalAlignment.Center),
                        new Column(
                            new Container(
                                new TextBlock(job.Description ?? job.Status.ToProgressStyle().Message)
                                    .WithColor(job.Status.ToProgressStyle().Color)
                                    .WithSize(TextSize.Small)
                                    .WithSpacing(Spacing.Small)
                                    .WithIsVisible(job.Description is not null)
                                    .WithIsSubtle(true)
                                    .WithWrap(false),
                                new TextBlock($"__{job.StatusMessage ?? job.Status.ToProgressStyle().Message}__")
                                    .WithColor(job.Status.ToProgressStyle().Color)
                                    .WithSize(TextSize.Small)
                                    .WithSpacing(Spacing.Small)
                                    .WithIsVisible(job.StatusMessage is not null)
                                    .WithIsSubtle(true)
                                    .WithWrap(false)
                                    .WithWeight(TextWeight.Bolder)
                            )
                        )
                        .WithWidth(new Union<string, float>("stretch"))
                        .WithVerticalContentAlignment(VerticalAlignment.Center),
                        new Column(
                            new TextBlock($"{elapse?.Seconds}s")
                                .WithColor(job.Status.ToProgressStyle().Color)
                                .WithIsSubtle(true)
                                .WithHorizontalAlignment(HorizontalAlignment.Right)
                        )
                        .WithIsVisible(elapse is not null)
                        .WithWidth(new Union<string, float>("auto"))
                        .WithVerticalContentAlignment(VerticalAlignment.Center)
                    )
                    .WithRoundedCorners(true)
                    .WithShowBorder(true)
                    .WithStyle(job.Status.ToProgressStyle().ContainerStyle);
                }).ToList<CardElement>()
            ])
        );
    }
}