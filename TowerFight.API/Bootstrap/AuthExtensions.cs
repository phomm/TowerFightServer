using Microsoft.AspNetCore.Authentication;

namespace TowerFight.API.Bootstrap;

public static class AuthExtensions
{
    public const string BasicAuthenticationScheme = "BasicAuthentication";

    public static IServiceCollection CustomizeAuthorization(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddAuthentication(BasicAuthenticationScheme)
            .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(BasicAuthenticationScheme, null);

        // 2. Build Authorization Policies
        serviceCollection.AddAuthorizationBuilder()
            .AddPolicy(Policies.Admin, policy => 
            {
                policy.AuthenticationSchemes.Add(BasicAuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.RequireRole(Policies.Admin);
            });

        return serviceCollection;
    }
}

public static class Policies
{
    public const string Admin = nameof(Admin);
}