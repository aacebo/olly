using FluentMigrator;

namespace OS.Agent.Storage.Migrations;

[Migration(5)]
public class CreateEntitiesTable : Migration
{
    public override void Up()
    {
        Create.Table("entities")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().ForeignKey("tenants", "id").NotNullable()
            .WithColumn("account_id").AsGuid().ForeignKey("accounts", "id").Nullable()
            .WithColumn("parent_id").AsGuid().ForeignKey("entities", "id").Nullable()
            .WithColumn("source_id").AsString().NotNullable()
            .WithColumn("source_type").AsString().NotNullable()
            .WithColumn("type").AsString().Nullable()
            .WithColumn("name").AsString().Nullable()
            .WithColumn("data").AsCustom("JSONB").NotNullable()
            .WithColumn("notes").AsCustom("JSONB").NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.UniqueConstraint()
            .OnTable("entities")
            .Columns("tenant_id", "source_id", "source_type");

        Create.Index()
            .OnTable("entities")
            .OnColumn("tenant_id").Ascending()
            .OnColumn("created_at").Descending();

        Create.Index()
            .OnTable("entities")
            .OnColumn("tenant_id").Ascending()
            .OnColumn("source_type").Ascending()
            .OnColumn("created_at").Descending();
    }

    public override void Down()
    {
        Delete.Table("entities");
    }
}