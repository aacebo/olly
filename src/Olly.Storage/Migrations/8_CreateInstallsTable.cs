using FluentMigrator;

namespace Olly.Storage.Migrations;

[Migration(8)]
public class CreateInstallsTable : Migration
{
    public override void Up()
    {
        Create.Table("installs")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("user_id").AsGuid().ForeignKey("users", "id").OnDelete(System.Data.Rule.Cascade).NotNullable()
            .WithColumn("account_id").AsGuid().ForeignKey("accounts", "id").OnDelete(System.Data.Rule.Cascade).NotNullable()
            .WithColumn("message_id").AsGuid().ForeignKey("messages", "id").Nullable()
            .WithColumn("source_type").AsString().NotNullable()
            .WithColumn("source_id").AsString().NotNullable()
            .WithColumn("status").AsString().NotNullable()
            .WithColumn("url").AsString().Nullable()
            .WithColumn("access_token").AsString().Nullable()
            .WithColumn("expires_at").AsDateTimeOffset().Nullable()
            .WithColumn("entities").AsCustom("JSONB").NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.UniqueConstraint()
            .OnTable("installs")
            .Columns("user_id", "account_id");

        Create.UniqueConstraint()
            .OnTable("installs")
            .Columns("source_id", "source_type");

        Create.Index()
            .OnTable("installs")
            .OnColumn("user_id").Ascending()
            .OnColumn("status").Ascending();
    }

    public override void Down()
    {
        Delete.Table("installs");
    }
}