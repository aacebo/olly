using FluentMigrator;

namespace OS.Agent.Storage.Migrations;

[Migration(10)]
public class CreateDocumentsTable : Migration
{
    public override void Up()
    {
        Create.Table("documents")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("record_id").AsGuid().ForeignKey("records", "id").OnDelete(System.Data.Rule.Cascade).NotNullable()
            .WithColumn("name").AsString().NotNullable()
            .WithColumn("path").AsString().NotNullable()
            .WithColumn("url").AsString().NotNullable()
            .WithColumn("size").AsInt64().NotNullable()
            .WithColumn("encoding").AsString().Nullable()
            .WithColumn("content").AsString().NotNullable()
            .WithColumn("embedding").AsCustom("vector(1536)").Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable();

        Create.UniqueConstraint()
            .OnTable("documents")
            .Columns("record_id", "path");
    }

    public override void Down()
    {
        Delete.Table("documents");
    }
}