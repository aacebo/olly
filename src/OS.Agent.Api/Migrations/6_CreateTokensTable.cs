using FluentMigrator;

namespace OS.Agent.Migrations;

[Migration(6)]
public class CreateTokensTable : Migration
{
    public override void Up()
    {
        Create.Table("tokens")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("account_id").AsGuid().ForeignKey("accounts", "id").Nullable()
            .WithColumn("type").AsString().Nullable()
            .WithColumn("access_token").AsString().Nullable()
            .WithColumn("refresh_token").AsString().Nullable()
            .WithColumn("expires_at").AsDateTimeOffset().Nullable()
            .WithColumn("refresh_expires_at").AsDateTimeOffset().Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("tokens");
    }
}