using System.Text.Json;

using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Cards;

using Olly.Errors;

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

        return new AdaptiveCard(
            new CodeBlock()
                .WithLanguage(CodeLanguage.Json)
                .WithCodeSnippet(JsonSerializer.Serialize(message, new JsonSerializerOptions()
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                }))
        );
    }
}