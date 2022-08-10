using Microsoft.AspNetCore.HttpOverrides;
using Prometheus;
using XLWebServices;
using XLWebServices.Services;
using XLWebServices.Services.PluginData;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<FallibleService<RedisService>>();
builder.Services.AddSingleton<DiscordHookService>();
builder.Services.AddSingleton<GitHubService>();
builder.Services.AddSingleton<FileCacheService>();
builder.Services.AddSingleton<FallibleService<PluginDataService>>();
builder.Services.AddSingleton<FallibleService<LauncherReleaseDataService>>();
builder.Services.AddSingleton<FallibleService<AssetCacheService>>();
builder.Services.AddSingleton<FallibleService<DalamudReleaseDataService>>();

builder.Services.AddResponseCaching();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

//app.MapControllers();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseRouting();

app.UseResponseCaching();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapMetrics();
});

// Initialize services
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Starting XLWebServices {Version}", Util.GetGitHash());

var discord = app.Services.GetRequiredService<DiscordHookService>();

var redis = app.Services.GetRequiredService<FallibleService<RedisService>>();
if (redis.HasFailed)
    await discord.SendError("Couldn't connect to redis", "Redis");

app.Services.GetRequiredService<GitHubService>();

var acs = app.Services.GetRequiredService<FallibleService<AssetCacheService>>();
await acs.RunFallibleAsync(s => s.ClearCache());

var drs = app.Services.GetRequiredService<FallibleService<DalamudReleaseDataService>>();
await drs.RunFallibleAsync(s => s.ClearCache());

var pds = app.Services.GetRequiredService<FallibleService<PluginDataService>>();
await pds.RunFallibleAsync(s => s.ClearCache());

var rds = app.Services.GetRequiredService<FallibleService<LauncherReleaseDataService>>();
await rds.RunFallibleAsync(s => s.ClearCache());

app.Run();