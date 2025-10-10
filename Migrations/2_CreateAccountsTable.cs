using FluentMigrator;

namespace OS.Agent.Migrations;

[Migration(2)]
public class CreateAccountsTable : Migration
{
    public override void Up()
    {
        Create.Table("accounts")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("user_id").AsGuid().ForeignKey("users", "id").NotNullable()
            .WithColumn("tenant_id").AsGuid().ForeignKey("tenants", "id").NotNullable()
            .WithColumn("source_id").AsString().NotNullable()
            .WithColumn("source_type").AsString().NotNullable()
            .WithColumn("name").AsString().NotNullable()
            .WithColumn("data").AsCustom("JSONB").NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.UniqueConstraint()
            .OnTable("accounts")
            .Columns("tenant_id", "source_id", "source_type");
    }

    public override void Down()
    {
        Delete.Table("accounts");
    }
}