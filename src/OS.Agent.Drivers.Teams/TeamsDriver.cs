using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;

using OS.Agent.Drivers.Models;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Teams;

public partial class TeamsDriver(IServiceProvider provider) : IChatDriver
{
    public SourceType Type => SourceType.Teams;

    private App Teams { get; init; } = provider.GetRequiredService<App>();

    public Task Install(InstallRequest request, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task UnInstall(UnInstallRequest request, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public async Task SignIn(SignInRequest request, CancellationToken cancellationToken = default)
    {
        var chatType = request.Chat.Type is null ? Microsoft.Teams.Api.ConversationType.Personal : new(request.Chat.Type);

        await Teams.Send(
            request.Chat.SourceId,
            new MessageActivity()
            {
                InputHint = Microsoft.Teams.Api.InputHint.AcceptingInput,
                Conversation = new()
                {
                    Id = request.Chat.SourceId,
                    Type = chatType,
                    Name = request.Chat.Name
                }
            }.AddAttachment(Cards.Auth.SignIn($"{request.Url}&state={request.State}")),
            chatType,
            request.Chat.Url,
            cancellationToken
        );
    }
}