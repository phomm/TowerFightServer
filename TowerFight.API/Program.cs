using Microsoft.AspNetCore.Authentication;
using TowerFight.API.Bootstrap;
using TowerFight.API.Health;
using TowerFight.BusinessLogic.Data;
using TowerFight.BusinessLogic.Data.Config;
using TowerFight.BusinessLogic.Data.Redis;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using HealthChecks.UI.Client;
using System.Text.Json.Serialization;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.SetupConfiguration();
builder.Services.AddConfigOptionsAndBind<RedisSettings>(builder.Configuration, nameof(RedisSettings), out var redisSettings);
builder.AddConfigOptions<DbSettings>();

// Add services to the container.
builder.Services
    .AddHealth(builder.Configuration)
    .AddHttpContextAccessor()
    .CustomizeAuthorization()
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    });
builder.Services
    .AddLogging() // Add logging from standard libraries
    .AddProblemDetails()
    .AddSwagger()
    .AddInternalServices()
    .AddDbServices(builder.Configuration)
    .AddRedis(redisSettings)    
    .AddCorsAllowsAny();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    var dbContextFactory = app.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
    await using var dbContext = await dbContextFactory.CreateDbContextAsync();
    app.Logger.LogInformation("Checking and applying pending database migrations.");
    await dbContext.Database.MigrateAsync();
}

app.MapHealthChecks(HealthCheckConsts.EndpointPath, new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

app.UseHttpsRedirection();
app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/swagger"))
    {
        await next();
        return;
    }

    var authentication = await context.AuthenticateAsync(AuthExtensions.BasicAuthenticationScheme);
    if (!authentication.Succeeded || authentication.Principal is null)
    {
        await context.ChallengeAsync(AuthExtensions.BasicAuthenticationScheme);
        return;
    }

    context.User = authentication.Principal;
    await next();
});
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.DocumentTitle = "TowerFight API Documentation";
    options.EnableDeepLinking();
});

app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.StartRedis(redisSettings.UseInMemoryCache);
app.Run();
