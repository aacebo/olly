using FluentMigrator;

namespace OS.Agent.Storage.Migrations;

[Migration(8)]
public class CreateInstallsTable : Migration
{
    public override void Up()
    {
        Create.Table("installs")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("account_id").AsGuid().ForeignKey("accounts", "id").NotNullable()
            .WithColumn("source_type").AsString().NotNullable()
            .WithColumn("source_id").AsString().NotNullable()
            .WithColumn("url").AsString().Nullable()
            .WithColumn("access_token").AsString().Nullable()
            .WithColumn("expires_at").AsDateTimeOffset().Nullable()
            .WithColumn("entities").AsCustom("JSONB").NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.UniqueConstraint()
            .OnTable("installs")
            .Columns("source_id", "source_type");
    }

    public override void Down()
    {
        Delete.Table("installs");
    }
}