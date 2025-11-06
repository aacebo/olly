using FluentMigrator;

namespace Olly.Storage.Migrations;

[Migration(10)]
public class CreateJobsTable : Migration
{
    public override void Up()
    {
        Create.Table("jobs")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("install_id").AsGuid().ForeignKey("installs", "id").OnDelete(System.Data.Rule.Cascade).NotNullable()
            .WithColumn("parent_id").AsGuid().ForeignKey("jobs", "id").OnDelete(System.Data.Rule.Cascade).Nullable()
            .WithColumn("chat_id").AsGuid().ForeignKey("chats", "id").OnDelete(System.Data.Rule.Cascade).Nullable()
            .WithColumn("message_id").AsGuid().ForeignKey("messages", "id").OnDelete(System.Data.Rule.Cascade).Nullable()
            .WithColumn("type").AsString().NotNullable()
            .WithColumn("name").AsString().NotNullable()
            .WithColumn("title").AsString().NotNullable()
            .WithColumn("description").AsString().Nullable()
            .WithColumn("entities").AsCustom("JSONB").NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("jobs");
    }
}