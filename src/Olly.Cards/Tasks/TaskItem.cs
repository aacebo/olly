using System.Text.Json.Serialization;

using Olly.Cards.Progress;

namespace Olly.Cards.Tasks;

public class TaskItem
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; } = Guid.NewGuid();

    [JsonPropertyName("style")]
    public required ProgressStyle Style { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("message")]
    public required string Message { get; set; }

    [JsonPropertyName("started_at")]
    public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("ended_at")]
    public DateTimeOffset? EndedAt { get; set; }

    public void Apply(Update update)
    {
        if (update.Style is not null)
        {
            Style = update.Style;
        }

        if (update.Title is not null)
        {
            Title = update.Title;
        }

        if (update.Message is not null)
        {
            Message = update.Message;
        }

        if (update.EndedAt is not null)
        {
            EndedAt = update.EndedAt;
        }
    }

    public class Create
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [JsonPropertyName("style")]
        public ProgressStyle Style { get; set; } = ProgressStyle.InProgress;

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("message")]
        public required string Message { get; set; }
    }

    public class Update
    {
        [JsonPropertyName("style")]
        public ProgressStyle? Style { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("ended_at")]
        public DateTimeOffset? EndedAt { get; set; }
    }
}