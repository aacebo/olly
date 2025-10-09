using FluentMigrator;

namespace OS.Agent.Migrations;

[Migration(1)]
public class CreateAccountsTable : Migration
{
    public override void Up()
    {
        Create.Table("accounts")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("user_id").AsGuid().ForeignKey("users", "id").NotNullable()
            .WithColumn("external_id").AsString().NotNullable()
            .WithColumn("type").AsString().NotNullable()
            .WithColumn("name").AsString().NotNullable()
            .WithColumn("data").AsCustom("JSONB").NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.UniqueConstraint()
            .OnTable("accounts")
            .Columns("external_id", "type");
    }

    public override void Down()
    {
        Delete.Table("accounts");
    }
}