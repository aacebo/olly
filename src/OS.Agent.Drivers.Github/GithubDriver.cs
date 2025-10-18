using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.Api.Activities;

using Octokit.GraphQL;
using Octokit.GraphQL.Model;

using OS.Agent.Drivers.Github.Models;
using OS.Agent.Services;
using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Github;

public class GithubDriver(IServiceProvider provider) : IChatDriver
{
    public SourceType Type => SourceType.Github;

    private Octokit.GitHubClient AppClient => provider.GetRequiredService<Octokit.GitHubClient>();
    private IAccountService Accounts => provider.GetRequiredService<IAccountService>();

    public async Task<TActivity> Send<TActivity>(Account account, TActivity activity, CancellationToken cancellationToken = default) where TActivity : IActivity
    {
        if (activity is not MessageActivity)
        {
            return activity;
        }

        if (activity.ReplyToId is not null && activity.Conversation.Type == "discussion")
        {
            activity.ReplyToId = null;
        }

        var entity = account.Entities.GetRequired<GithubAccountInstallEntity>();

        if (entity.AccessToken.ExpiresAt >= DateTimeOffset.UtcNow.AddMinutes(-5))
        {
            entity.AccessToken = await AppClient.GitHubApps.CreateInstallationToken(entity.Install.Id);
            await Accounts.Update(account, cancellationToken);
        }

        var client = new Connection(new ProductHeaderValue("TOS-Agent"), entity.AccessToken.Token);
        var query = new Mutation()
            .AddDiscussionComment(new AddDiscussionCommentInput()
            {
                DiscussionId = new ID(activity.Conversation.Id),
                ReplyToId = activity.ReplyToId is not null ? new(activity.ReplyToId) : null,
                Body = activity is MessageActivity message ? message.Text : string.Empty
            })
            .Select(res => new GithubDiscussionComment()
            {
                Id = res.Comment.Id,
                Url = res.Comment.Url,
                Body = res.Comment.Body,
                UpVotes = res.Comment.UpvoteCount
            })
            .Compile();

        var comment = await client.Run(
            query,
            cancellationToken: cancellationToken
        );

        activity.Id = comment.Id.ToString();
        activity.ChannelData ??= new();
        activity.ChannelData.Properties["github"] = comment;
        return activity;
    }
}