using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace TowerFight.API.Bootstrap;

public class BasicAuthenticationHandler(IConfiguration config, IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) 
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var value))
            return Task.FromResult(AuthenticateResult.NoResult());

        if (!AuthenticationHeaderValue.TryParse(value.ToString(), out var authHeader) ||
            !string.Equals(authHeader.Scheme, "Basic", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrEmpty(authHeader.Parameter))
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));

        string credentials;
        try
        {
            credentials = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader.Parameter));
        }
        catch (FormatException)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));
        }

        var separatorIndex = credentials.IndexOf(':');
        if (separatorIndex < 0)
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));

        var username = credentials[..separatorIndex];
        var password = credentials[(separatorIndex + 1)..];
        var validUsername = config["BasicAuth:Username"];
        var validPassword = config["BasicAuth:Password"];

        if (!string.Equals(username, validUsername, StringComparison.Ordinal) ||
            !string.Equals(password, validPassword, StringComparison.Ordinal))
            return Task.FromResult(AuthenticateResult.Fail("Invalid Username or Password"));

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, Policies.Admin)
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = "Basic realm=\"TowerFight API\"";
        return base.HandleChallengeAsync(properties);
    }
}