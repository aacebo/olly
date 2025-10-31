using FluentMigrator;

namespace Olly.Storage.Migrations;

[Migration(6)]
public class CreateLogsTable : Migration
{
    public override void Up()
    {
        Create.Table("logs")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().ForeignKey("tenants", "id").OnDelete(System.Data.Rule.Cascade).NotNullable()
            .WithColumn("level").AsString().NotNullable()
            .WithColumn("type").AsString().NotNullable()
            .WithColumn("type_id").AsString().Nullable()
            .WithColumn("text").AsString().NotNullable()
            .WithColumn("entities").AsCustom("JSONB").NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable();

        Create.Index()
            .OnTable("logs")
            .OnColumn("tenant_id").Ascending()
            .OnColumn("level").Ascending()
            .OnColumn("type").Ascending()
            .OnColumn("created_at").Descending();
    }

    public override void Down()
    {
        Delete.Table("logs");
    }
}