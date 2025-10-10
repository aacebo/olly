using FluentMigrator;

namespace OS.Agent.Migrations;

[Migration(5)]
public class CreateEntitiesTable : Migration
{
    public override void Up()
    {
        Create.Table("entities")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().ForeignKey("tenants", "id").NotNullable()
            .WithColumn("account_id").AsGuid().ForeignKey("accounts", "id")
            .WithColumn("parent_id").AsGuid().ForeignKey("entities", "id")
            .WithColumn("source_id").AsString().NotNullable()
            .WithColumn("source_type").AsString().NotNullable()
            .WithColumn("type").AsString()
            .WithColumn("name").AsString()
            .WithColumn("data").AsCustom("JSONB").NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.UniqueConstraint()
            .OnTable("entities")
            .Columns("tenant_id", "source_id", "source_type");
    }

    public override void Down()
    {
        Delete.Table("entities");
    }
}