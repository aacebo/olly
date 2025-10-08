using System.ComponentModel.DataAnnotations.Schema;

namespace OS.Agent.Models;

[Table("installations")]
public class Installation
{
    [Column("id")]
    public long Id { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public record Create
    {
        public required long Id { get; init; }
    }

    public record Update
    {
    }
}