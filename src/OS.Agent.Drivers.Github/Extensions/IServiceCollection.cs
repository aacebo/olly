using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Octokit;

using OS.Agent.Drivers.Github.Settings;

namespace OS.Agent.Drivers.Github.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddGithubDriver(this IServiceCollection services)
    {
        var provider = services.BuildServiceProvider();
        var jsonOptions = provider.GetRequiredService<JsonSerializerOptions>();

        services.AddScoped<GithubDriver>();
        services.AddScoped<GithubService>();
        services.AddScoped<IDriver>(provider => provider.GetRequiredService<GithubDriver>());
        services.AddScoped<IChatDriver>(provider => provider.GetRequiredService<GithubDriver>());
        services.AddSingleton(provider =>
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

            return new Octokit.GraphQL.Connection(
                new Octokit.GraphQL.ProductHeaderValue("TOS-Agent"),
                handler.WriteToken(token)
            );
        });

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

            var connection = new Connection(new ProductHeaderValue("TOS-Agent"))
            {
                Credentials = new Credentials(
                    handler.WriteToken(token),
                    AuthenticationType.Bearer
                )
            };

            return new GitHubClient(connection);
        });
    }
}