using Microsoft.AspNetCore.Authentication;

namespace TowerFight.API.Bootstrap;

public static class AuthExtensions
{
    public static IServiceCollection CustomizeAuthorization(this IServiceCollection serviceCollection)
    {
        const string SchemaName = "BasicAuthentication";
        serviceCollection.AddAuthentication(SchemaName)
            .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(SchemaName, null);

        // 2. Build Authorization Policies
        serviceCollection.AddAuthorizationBuilder()
            .AddPolicy(Policies.Admin, policy => 
            {
                policy.AuthenticationSchemes.Add(SchemaName);
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