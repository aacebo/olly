using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.Api.Activities;

using Octokit.GraphQL;
using Octokit.GraphQL.Model;

using OS.Agent.Storage.Models;

namespace OS.Agent.Drivers.Github;

public class GithubDriver(IServiceProvider provider) : IChatDriver
{
    public SourceType Type => SourceType.Github;

    private Connection GraphQL { get; init; } = provider.GetRequiredService<Connection>();

    public async Task<IActivity> Send(IActivity activity, CancellationToken cancellationToken = default)
    {
        if (activity is not MessageActivity)
        {
            return activity;
        }

        var comment = await GraphQL.Run(
            new Mutation()
                .AddDiscussionComment(new AddDiscussionCommentInput()
                {
                    DiscussionId = new ID(activity.Conversation.Id),
                    ReplyToId = activity.ReplyToId is not null ? new(activity.ReplyToId) : null,
                    Body = activity is MessageActivity message ? message.Text : string.Empty
                })
                .Select(res => res.Comment)
                .Compile(),
            cancellationToken: cancellationToken
        );

        activity.Id = comment.Id.ToString();
        return activity;
    }
}