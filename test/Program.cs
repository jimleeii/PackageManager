using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using PackageManager.Configuration;
using Test;

var builder = WebApplication.CreateBuilder(args);

// Add PackageManager services with file watching
builder.Services.AddPackageManager(builder.Configuration);

var app = builder.Build();

// Install all packages from the configured directory
await app.UsePackageManager();

app.MapGet("/demo", async () =>
{
    await RepositoryDemo.DemoRepositoryUsage(app);
    return Results.Ok("Demo completed. Check console output for details.");
});

await app.RunAsync();