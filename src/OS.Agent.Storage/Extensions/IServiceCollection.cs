using System.Data;
using System.Reflection;
using System.Text.Json;

using FluentMigrator.Runner;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Npgsql;

using OS.Agent.Storage.Models;
using OS.Agent.Storage.Postgres;

using Pgvector.Dapper;

using SqlKata;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace OS.Agent.Storage.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddPostgres(this IServiceCollection services)
    {
        // load model type mappings
        var provider = services.BuildServiceProvider();
        var jsonOptions = provider.GetRequiredService<JsonSerializerOptions>();
        var modelTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => type.GetCustomAttribute<ModelAttribute>() != null);

        Dapper.SqlMapper.AddTypeHandler(new VectorTypeHandler());
        Dapper.SqlMapper.AddTypeHandler(new StringEnumTypeHandler<SourceType>());
        Dapper.SqlMapper.AddTypeHandler(new StringEnumTypeHandler<Models.LogLevel>());
        Dapper.SqlMapper.AddTypeHandler(new StringEnumTypeHandler<LogType>());
        Dapper.SqlMapper.AddTypeHandler(new StringEnumTypeHandler<InstallStatus>());
        Dapper.SqlMapper.AddTypeHandler(new StringEnumTypeHandler<JobStatus>());
        Dapper.SqlMapper.AddTypeHandler(typeof(List<Source>), new JsonArrayTypeHandler(jsonOptions));
        Dapper.SqlMapper.AddTypeHandler(typeof(Entities), new JsonArrayTypeHandler(jsonOptions));
        Dapper.SqlMapper.AddTypeHandler(typeof(List<Attachment>), new JsonArrayTypeHandler(jsonOptions));

        foreach (var type in modelTypes)
        {
            Dapper.SqlMapper.SetTypeMap(type, new Dapper.CustomPropertyTypeMap
            (
                type,
                (type, name) =>
                {
                    var property = type.GetProperties().FirstOrDefault(p =>
                        p.GetCustomAttributes()
                            .OfType<ColumnAttribute>()
                            .Any(attr => attr.Name == name)
                    );

                    return property ?? throw new Exception($"property '{name}' not found on type '{type.FullName}'");
                }
            ));
        }

        // add migrations
        services.AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithMigrationsIn(Assembly.GetExecutingAssembly())
                .WithGlobalConnectionString("Postgres")
            );

        // add database connection
        services.AddSingleton(provider =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            var builder = new NpgsqlDataSourceBuilder(config.GetConnectionString("Postgres"));

            builder.UseVector();
            return builder
                .EnableDynamicJson()
                .ConfigureJsonOptions(jsonOptions)
                .Build();
        });

        // add query factory
        return services.AddScoped(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<QueryFactory>>();
            var dataSource = provider.GetRequiredService<NpgsqlDataSource>();
            var jsonSerializerOptions = provider.GetRequiredService<JsonSerializerOptions>();
            var conn = dataSource.OpenConnection();

            conn.ReloadTypes();

            return new QueryFactory(conn, new PostgresCompiler())
            {
                Logger = q => logger.LogDebug("{}", q.RawSql)
            };
        });
    }
}