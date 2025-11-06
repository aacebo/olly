using FluentMigrator;

namespace Olly.Storage.Migrations;

[Migration(12)]
public class CreateJobRunsTable : Migration
{
    public override void Up()
    {
        Create.Table("job_runs")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("job_id").AsGuid().ForeignKey("jobs", "id").OnDelete(System.Data.Rule.Cascade).NotNullable()
            .WithColumn("status").AsString().NotNullable()
            .WithColumn("status_message").AsString().Nullable()
            .WithColumn("started_at").AsDateTimeOffset().Nullable()
            .WithColumn("ended_at").AsDateTimeOffset().Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Alter.Table("jobs")
            .AddColumn("last_run_id")
            .AsGuid()
            .ForeignKey("job_runs", "id")
            .Nullable();
    }

    public override void Down()
    {
        Delete.Table("job_runs");
    }
}