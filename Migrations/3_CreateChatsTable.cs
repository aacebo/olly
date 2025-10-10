using FluentMigrator;

namespace OS.Agent.Migrations;

[Migration(3)]
public class CreateChatsTable : Migration
{
    public override void Up()
    {
        Create.Table("chats")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().ForeignKey("tenants", "id").NotNullable()
            .WithColumn("parent_id").AsGuid().ForeignKey("chats", "id")
            .WithColumn("source_id").AsString().NotNullable()
            .WithColumn("source_type").AsString().NotNullable()
            .WithColumn("name").AsString()
            .WithColumn("data").AsCustom("JSONB").NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.UniqueConstraint()
            .OnTable("chats")
            .Columns("tenant_id", "source_id", "source_type");
    }

    public override void Down()
    {
        Delete.Table("chats");
    }
}