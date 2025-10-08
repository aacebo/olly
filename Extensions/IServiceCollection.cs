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
                expires: DateTime.UtcNow.AddMinutes(10),
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
            .ConfigureRunner(rb => rb.AddPostgres().WithMigrationsIn(Assembly.GetExecutingAssembly()));

        return services.AddSingleton(provider =>
        {
            var connection = new NpgsqlConnection(url);
            return new QueryFactory(connection, new PostgresCompiler());
        });
    }
}