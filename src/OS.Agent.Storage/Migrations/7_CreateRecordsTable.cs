using FluentMigrator;

namespace OS.Agent.Storage.Migrations;

[Migration(7)]
public class CreateRecordsTable : Migration
{
    public override void Up()
    {
        Create.Table("records")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().ForeignKey("tenants", "id").NotNullable()
            .WithColumn("parent_id").AsGuid().ForeignKey("records", "id").Nullable()
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
            .Columns("tenant_id", "source_id", "source_type");
    }

    public override void Down()
    {
        Delete.Table("records");
    }
}