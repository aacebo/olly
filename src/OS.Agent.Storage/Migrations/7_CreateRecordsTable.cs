using FluentMigrator;

namespace OS.Agent.Storage.Migrations;

[Migration(7)]
public class CreateRecordsTable : Migration
{
    public override void Up()
    {
        Create.Table("records")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("parent_id").AsGuid().ForeignKey("records", "id").OnDelete(System.Data.Rule.Cascade).Nullable()
            .WithColumn("source_id").AsString().NotNullable()
            .WithColumn("source_type").AsString().NotNullable()
            .WithColumn("url").AsString().Nullable()
            .WithColumn("type").AsString().NotNullable()
            .WithColumn("name").AsString().Nullable()
            .WithColumn("entities").AsCustom("JSONB").NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.UniqueConstraint()
            .OnTable("records")
            .Columns("source_id", "source_type");

        Create.Index()
            .OnTable("records")
            .OnColumn("source_type").Ascending()
            .OnColumn("type").Ascending()
            .OnColumn("updated_at").Descending();

        Create.Table("tenants_records")
            .WithColumn("tenant_id").AsGuid().ForeignKey("tenants", "id").OnDelete(System.Data.Rule.Cascade).NotNullable()
            .WithColumn("record_id").AsGuid().ForeignKey("records", "id").OnDelete(System.Data.Rule.Cascade).NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable();

        Create.PrimaryKey()
            .OnTable("tenants_records")
            .Columns("tenant_id", "record_id");

        Create.Table("accounts_records")
            .WithColumn("account_id").AsGuid().ForeignKey("accounts", "id").OnDelete(System.Data.Rule.Cascade).NotNullable()
            .WithColumn("record_id").AsGuid().ForeignKey("records", "id").OnDelete(System.Data.Rule.Cascade).NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable();

        Create.PrimaryKey()
            .OnTable("accounts_records")
            .Columns("account_id", "record_id");

        Create.Table("chats_records")
            .WithColumn("chat_id").AsGuid().ForeignKey("chats", "id").OnDelete(System.Data.Rule.Cascade).NotNullable()
            .WithColumn("record_id").AsGuid().ForeignKey("records", "id").OnDelete(System.Data.Rule.Cascade).NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable();

        Create.PrimaryKey()
            .OnTable("chats_records")
            .Columns("chat_id", "record_id");

        Create.Table("messages_records")
            .WithColumn("message_id").AsGuid().ForeignKey("messages", "id").OnDelete(System.Data.Rule.Cascade).NotNullable()
            .WithColumn("record_id").AsGuid().ForeignKey("records", "id").OnDelete(System.Data.Rule.Cascade).NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable();

        Create.PrimaryKey()
            .OnTable("messages_records")
            .Columns("message_id", "record_id");
    }

    public override void Down()
    {
        Delete.Table("records");
    }
}