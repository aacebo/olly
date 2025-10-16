using FluentMigrator;

namespace OS.Agent.Storage.Migrations;

[Migration(4)]
public class CreateMessagesTable : Migration
{
    public override void Up()
    {
        Create.Table("messages")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("chat_id").AsGuid().ForeignKey("chats", "id").NotNullable()
            .WithColumn("account_id").AsGuid().ForeignKey("accounts", "id").Nullable()
            .WithColumn("reply_to_id").AsGuid().ForeignKey("messages", "id").Nullable()
            .WithColumn("source_id").AsString().NotNullable()
            .WithColumn("source_type").AsString().NotNullable()
            .WithColumn("text").AsString().NotNullable()
            .WithColumn("data").AsCustom("JSONB").NotNullable()
            .WithColumn("notes").AsCustom("JSONB").NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.UniqueConstraint()
            .OnTable("messages")
            .Columns("chat_id", "source_id", "source_type");

        Create.Index()
            .OnTable("messages")
            .OnColumn("chat_id").Ascending()
            .OnColumn("reply_to_id").Ascending()
            .OnColumn("source_type").Ascending();

        Create.Index()
            .OnTable("messages")
            .OnColumn("chat_id").Ascending()
            .OnColumn("created_at").Descending();
    }

    public override void Down()
    {
        Delete.Table("messages");
    }
}