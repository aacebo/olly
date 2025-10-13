using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;

using FluentMigrator.Runner;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Npgsql;

using Octokit;

using OS.Agent.Models;
using OS.Agent.Postgres;
using OS.Agent.Settings;

using SqlKata;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace OS.Agent.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddGithubClient(this IServiceCollection services)
    {
        return services.AddSingleton(provider =>
        {
            var settings = provider.GetRequiredService<IOptions<GithubSettings>>();
            var pem = File.ReadAllText(@"github.private-key.pem");
            var rsa = RSA.Create();
            var time = new DateTimeOffset(DateTime.UtcNow);
            rsa.ImportFromPem(pem);

            var handler = new JwtSecurityTokenHandler();
            var token = handler.CreateJwtSecurityToken(
                subject: new ClaimsIdentity([]),
                expires: DateTime.UtcNow.AddMinutes(10).AddSeconds(-10),
                issuedAt: DateTime.UtcNow.AddSeconds(-60),
                signingCredentials: new SigningCredentials(
                    new RsaSecurityKey(rsa),
                    SecurityAlgorithms.RsaSha256
                ),
                issuer: settings.Value.ClientId
            );

            return new GitHubClient(new ProductHeaderValue("TOS-Agent"))
            {
                Credentials = new Credentials(
                    handler.WriteToken(token),
                    AuthenticationType.Bearer
                )
            };
        });
    }

    public static IServiceCollection AddPostgres(this IServiceCollection services)
    {
        // load model type mappings
        var modelTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => type.GetCustomAttribute<ModelAttribute>() != null);

        foreach (var type in modelTypes)
        {
            Dapper.SqlMapper.SetTypeMap(type, new Dapper.CustomPropertyTypeMap
            (
                type,
                (type, columnName) =>
                    type.GetProperties().FirstOrDefault(prop =>
                        prop.GetCustomAttributes(false)
                            .OfType<ColumnAttribute>()
                            .Any(attr => attr.Name == columnName)
                    ) ?? throw new Exception($"property '{columnName}' not found on type '{type.FullName}'")
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
        services.AddTransient(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<NpgsqlDataSource>>();
            var config = provider.GetRequiredService<IConfiguration>();
            var jsonOptions = provider.GetService<JsonSerializerOptions>();

            // load type handlers
            if (!Dapper.SqlMapper.HasTypeHandler(typeof(Data)))
            {
                Dapper.SqlMapper.AddTypeHandler(new DataTypeHandler<Data>(jsonOptions));
                Dapper.SqlMapper.AddTypeHandler(new DataTypeHandler<Data.Account>(jsonOptions));
                Dapper.SqlMapper.AddTypeHandler(new DataTypeHandler<Data.Account.Teams>(jsonOptions));
                Dapper.SqlMapper.AddTypeHandler(new DataTypeHandler<Data.Account.Github>(jsonOptions));
                Dapper.SqlMapper.AddTypeHandler(new DataTypeHandler<Data.Chat>(jsonOptions));
                Dapper.SqlMapper.AddTypeHandler(new DataTypeHandler<Data.Chat.Teams>(jsonOptions));
                Dapper.SqlMapper.AddTypeHandler(new DataTypeHandler<Data.Message>(jsonOptions));
                Dapper.SqlMapper.AddTypeHandler(new DataTypeHandler<Data.Message.Teams>(jsonOptions));
            }

            if (!Dapper.SqlMapper.HasTypeHandler(typeof(StringEnum)))
            {
                Dapper.SqlMapper.AddTypeHandler(new StringEnumTypeHandler<SourceType>());
            }

            if (!Dapper.SqlMapper.HasTypeHandler(typeof(Tenant.SourceList)))
            {
                Dapper.SqlMapper.AddTypeHandler(new ListTypeHandler(jsonOptions));
                // Dapper.SqlMapper.AddTypeHandler(new SourceTypeHandler(jsonOptions));
            }

            return new NpgsqlDataSourceBuilder(config.GetConnectionString("Postgres"))
                    .EnableDynamicJson()
                    .Build()
                    .CreateConnection();
        });

        // add query factory
        return services.AddScoped(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<QueryFactory>>();
            var connection = provider.GetRequiredService<NpgsqlConnection>();

            if (connection.State != ConnectionState.Open)
            {
                logger.LogInformation("opening connection...");
                connection.Open();
                logger.LogInformation("opened successfully!");
            }

            var factory = new QueryFactory(connection, new PostgresCompiler());
            factory.Logger = q => logger.LogDebug("{}", q);
            return factory;
        });
    }
}