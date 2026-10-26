using Microsoft.EntityFrameworkCore;
using Twiggle.Api;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
// This skeleton has no hosted identity provider. Fail closed outside local development.
if (!builder.Environment.IsDevelopment())
    throw new InvalidOperationException("Hosted use requires authentication and workspace authorization. This skeleton is local-development only.");
builder.WebHost.UseUrls("http://127.0.0.1:5080");
builder.Services.AddProblemDetails();
builder.Services.AddDbContext<TwiggleDbContext>(options => options.UseNpgsql(
    builder.Configuration.GetConnectionString("Twiggle")
    ?? throw new InvalidOperationException("Set ConnectionStrings__Twiggle before starting the API.")));
builder.Services.AddHttpClient<AiInterviewer>(http => http.Timeout = TimeSpan.FromSeconds(90));
builder.Services.AddSingleton<InterviewLocks>();
var app = builder.Build();
app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    try { await next(context); }
    catch (DbUpdateConcurrencyException) { await Conflict(context); }
    catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" }) { await Conflict(context); }
});
static Task Conflict(HttpContext context)
{
    context.Response.StatusCode = 409;
    return context.Response.WriteAsJsonAsync(new { message = "Record changed or operation ID was reused. Reload before retrying; keep your draft." });
}
if (builder.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<TwiggleDbContext>().Database.MigrateAsync();
}
app.MapExperiences();
app.MapAiInterview();
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
app.MapGet("/api/workspace", (AiInterviewer ai) => Results.Ok(new
{
    id = "local-development", displayName = "Local workspace", mode = "development-only",
    capabilities = new { interviews = true, persistence = true, ai = ai.Configured }
}));
app.Run();
