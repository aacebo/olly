using FluentMigrator;

namespace OS.Agent.Migrations;

[Migration(1)]
public class CreateTenantsTable : Migration
{
    public override void Up()
    {
        Create.Table("tenants")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("source_id").AsString().NotNullable()
            .WithColumn("source_type").AsString().NotNullable()
            .WithColumn("name").AsString()
            .WithColumn("data").AsCustom("JSONB").NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.UniqueConstraint()
            .OnTable("tenants")
            .Columns("source_id", "source_type");
    }

    public override void Down()
    {
        Delete.Table("tenants");
    }
}