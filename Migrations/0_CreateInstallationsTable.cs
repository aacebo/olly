using FluentMigrator;

namespace OS.Agent.Migrations;

[Migration(0)]
public class CreateInstallationsTable : Migration
{
    public override void Up()
    {
        Create.Table("installations")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("installations");
    }
}