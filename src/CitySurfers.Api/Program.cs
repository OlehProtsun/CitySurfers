using CitySurfers.Api.Errors;
using CitySurfers.Application;
using CitySurfers.Infrastructure;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddCors(options =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    if (origins.Any(origin => !Uri.TryCreate(origin, UriKind.Absolute, out var uri)
        || uri.Scheme is not ("http" or "https") || origin != uri.GetLeftPart(UriPartial.Authority)))
        throw new InvalidOperationException("Cors:AllowedOrigins must contain HTTP(S) origins without paths or trailing slashes.");
    options.AddDefaultPolicy(policy =>
    {
        if (origins.Length > 0)
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
    });
});
builder.Services.AddOptions<CorsOptions>().ValidateOnStart();

var app = builder.Build();
app.UseExceptionHandler(new ExceptionHandlerOptions { SuppressDiagnosticsCallback = _ => true });
app.UseStatusCodePages();
app.UseCors();
if (app.Environment.IsDevelopment())
    app.MapOpenApi();
app.MapControllers();
app.MapGet("/health", () => Results.Text("Healthy"))
    .WithSummary("API liveness");
app.MapGet("/health/ready", async (HealthCheckService health, CancellationToken cancellationToken) =>
{
    var report = await health.CheckHealthAsync(check => check.Tags.Contains("ready"), cancellationToken);
    return Results.Text(report.Status.ToString(), statusCode:
        report.Status == HealthStatus.Unhealthy ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status200OK);
}).WithSummary("MongoDB readiness").Produces(StatusCodes.Status200OK).Produces(StatusCodes.Status503ServiceUnavailable);
await app.Services.InitializeInfrastructureAsync(app.Lifetime.ApplicationStopping);
app.Run();

public partial class Program;
