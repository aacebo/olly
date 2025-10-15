using FluentMigrator;

namespace OS.Agent.Storage.Migrations;

[Migration(1)]
public class CreateTenantsTable : Migration
{
    public override void Up()
    {
        Create.Table("tenants")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("sources").AsCustom("JSONB").NotNullable()
            .WithColumn("name").AsString().Nullable()
            .WithColumn("data").AsCustom("JSONB").NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Execute.Sql("CREATE INDEX ON tenants USING GIN (sources);");
    }

    public override void Down()
    {
        Delete.Table("tenants");
    }
}