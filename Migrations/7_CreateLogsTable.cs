using FluentMigrator;

namespace OS.Agent.Migrations;

[Migration(7)]
public class CreateLogsTable : Migration
{
    public override void Up()
    {
        Create.Table("logs")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().ForeignKey("tenants", "id").NotNullable()
            .WithColumn("level").AsString().NotNullable()
            .WithColumn("type").AsString().NotNullable()
            .WithColumn("type_id").AsString().Nullable()
            .WithColumn("text").AsString().NotNullable()
            .WithColumn("data").AsCustom("JSONB").NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("logs");
    }
}