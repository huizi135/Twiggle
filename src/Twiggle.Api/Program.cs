using Microsoft.EntityFrameworkCore;
using Twiggle.Api;

var builder = WebApplication.CreateBuilder(args);
// This skeleton has no hosted identity provider. Fail closed outside local development.
if (!builder.Environment.IsDevelopment())
    throw new InvalidOperationException("Hosted use requires authentication and workspace authorization. This skeleton is local-development only.");
builder.WebHost.UseUrls("http://127.0.0.1:5080");
builder.Services.AddProblemDetails();
builder.Services.AddDbContext<TwiggleDbContext>(options => options.UseNpgsql(
    builder.Configuration.GetConnectionString("Twiggle")
    ?? throw new InvalidOperationException("Set ConnectionStrings__Twiggle before starting the API.")));
var app = builder.Build();
app.UseExceptionHandler();
app.MapGet("/api/health/live", () => Results.Ok(new { status = "healthy", service = "Twiggle.Api" }));
app.MapGet("/api/health/ready", async (TwiggleDbContext db, CancellationToken ct) =>
{
    try
    {
        return await db.Database.CanConnectAsync(ct)
            ? Results.Ok(new { status = "ready", database = "connected" })
            : Results.Problem(statusCode: 503, title: "Database unavailable");
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
        return Results.Problem(statusCode: 503, title: "Database unavailable");
    }
});
app.MapGet("/api/workspace", () => Results.Ok(new
{
    id = "local-development", displayName = "Local workspace", mode = "development-only",
    capabilities = new { interviews = false, persistence = false, ai = false }
}));
app.Run();
