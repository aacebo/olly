using FluentMigrator;

namespace Olly.Storage.Migrations;

[Migration(11)]
public class CreateJobApprovalsTable : Migration
{
    public override void Up()
    {
        Create.Table("job_approvals")
            .WithColumn("job_id").AsGuid().ForeignKey("jobs", "id").OnDelete(System.Data.Rule.Cascade).NotNullable()
            .WithColumn("account_id").AsGuid().ForeignKey("accounts", "id").OnDelete(System.Data.Rule.Cascade).NotNullable()
            .WithColumn("status").AsString().NotNullable()
            .WithColumn("required").AsBoolean().NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.PrimaryKey()
            .OnTable("job_approvals")
            .Columns("job_id", "account_id");
    }

    public override void Down()
    {
        Delete.Table("job_approvals");
    }
}