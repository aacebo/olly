using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;

using FluentMigrator.Runner;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Npgsql;

using Octokit;

using OS.Agent.Settings;

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

    public static IServiceCollection AddPostgres(this IServiceCollection services, string url)
    {
        services.AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithMigrationsIn(Assembly.GetExecutingAssembly())
                .WithGlobalConnectionString("Postgres")
            );

        services.AddTransient(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<NpgsqlDataSource>>();
            return new NpgsqlDataSourceBuilder(url)
                    .UseLoggerFactory(provider.GetRequiredService<ILoggerFactory>())
                    .EnableDynamicJson()
                    .Build()
                    .CreateConnection();
        });

        return services.AddTransient(provider =>
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