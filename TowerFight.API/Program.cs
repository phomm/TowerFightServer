using TowerFight.API.Bootstrap;
using TowerFight.API.Health;
using TowerFight.BusinessLogic.Data.Config;
using TowerFight.BusinessLogic.Data.Redis;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
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

app.MapHealthChecks(HealthCheckConsts.EndpointPath, new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.DocumentTitle = "TowerFight API Documentation";
    options.EnableDeepLinking();
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.StartRedis(redisSettings.UseInMemoryCache);
app.Run();
