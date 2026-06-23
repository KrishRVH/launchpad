using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

namespace Launchpad.Web.Features.Integrations;

public sealed class IntegrationApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder) {
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() {
        string? expected = configuration["Launchpad:IntegrationApiKey"];
        if (string.IsNullOrWhiteSpace(expected)) {
            return Task.FromResult(AuthenticateResult.Fail("Integration API key is not configured."));
        }

        if (!Request.Headers.TryGetValue(IntegrationApiKeyDefaults.HeaderName, out StringValues provided)) {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!SecretsEqual(provided.ToString(), expected)) {
            return Task.FromResult(AuthenticateResult.Fail("Invalid integration API key."));
        }

        Claim[] claims = [
            new(ClaimTypes.NameIdentifier, "integration-api"),
            new(ClaimTypes.Name, "Integration API"),
        ];
        ClaimsPrincipal principal = new(new ClaimsIdentity(claims, Scheme.Name));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }

    private static bool SecretsEqual(string actual, string expected) {
        byte[] actualHash = SHA256.HashData(Encoding.UTF8.GetBytes(actual));
        byte[] expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
