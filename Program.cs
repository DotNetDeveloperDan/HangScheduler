using Hangfire;
using Hangfire.Common;
using Hangfire.LiteDB;
using Hangfire.States;
using Hangfire.Storage;
using HangScheduler.Api;
using HangScheduler.Api.Filters;
using HangScheduler.Api.Interfaces;
using HangScheduler.Api.Middleware;
using HangScheduler.Api.Models;
using HangScheduler.Api.Utilities;
using Serilog;
using NSwag;                                   // still fine if you need it
using NSwag.Generation.Processors.Security;    // only if you use SecurityProcessors
using NSwag.Generation.AspNetCore;             // <-- NEW
using NSwag.AspNetCore;                        // <-- NEW


var builder = WebApplication.CreateBuilder(args);



var environment = builder.Environment;

builder.Configuration
    .AddJsonFile("appsettings.json", false, true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, true) // Load Staging config
    .AddEnvironmentVariables();


//Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration) // Load Serilog config from appsettings.json
    .CreateLogger();

builder.Host.UseSerilog();
builder.Host.UseSystemd();
// Add services to the container.
builder.Services.AddControllers(); // Add API controller support

// Hangfire setup for Lite Db storage

builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseLiteDbStorage("Filename=hangfire_lite.db;Mode=Exclusive;")
);

// Add the Hangfire server, which will act as the job scheduler
builder.Services.AddHangfireServer();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add Swagger and API Explorer
builder.Services.AddEndpointsApiExplorer();


builder.Services.AddScoped<IJobProcessor, JobProcessor>();
// Register HttpClient in DI
// Register HttpClient with SSL bypass in development
builder.Services.AddHttpClient("JobProcessorClient")
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();

        if (!environment.IsDevelopment()) return handler;

        handler.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        Log.Information("SSL validation bypassed in development.");

        return handler;
    });

builder.Services.AddOpenApiDocument(settings =>
{
    settings.Title = "My API";
    settings.Version = "v1";
    // settings.UseXmlDocumentation is true by default; no extra code needed
});


// Build the app
var app = builder.Build();
app.UseMiddleware<GlobalExceptionCatcher>();
// Middleware: Routing should come before Hangfire Dashboard
app.UseRouting(); // Ensures correct routing for endpoints

// Enable the Hangfire Dashboard for monitoring with an authorization filter
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new AllowAllDashboardAuthorizationFilter()] // Fix the array syntax
});

// Configure the HTTP request pipeline for development
if (!app.Environment.IsProduction())
{
    app.UseSwaggerUi(ui =>
    {
        ui.DocExpansion = "list";   // collapse sections by default
        ui.TagsSorter = "alpha";  // A-Z tag groups
        ui.OperationsSorter = "alpha";  // A-Z operations
    });

}


using (var scope = app.Services.CreateScope())
{
    var jobProcessor = scope.ServiceProvider.GetRequiredService<IJobProcessor>();
    // Use jobProcessor here
    // Define Minimal API endpoints
    app.MapPost("/jobs/schedule/now", async (JobRequest request, IBackgroundJobClient backgroundJobClient) =>
    {
        string jobId;

        if (!string.IsNullOrEmpty(request.ContinueAfterJobId))
            // Schedule as a continuation job
            jobId = backgroundJobClient.ContinueJobWith(
                request.ContinueAfterJobId,
                () => request.PreventConcurrentExecution
                    ? jobProcessor.CallExternalApiWithConcurrencyControl(request.ApiEndpoint, request.Payload)
                    : jobProcessor.CallExternalApi(request.ApiEndpoint, request.Payload));
        else
            // Schedule the job
            jobId = backgroundJobClient.Create(
                request.PreventConcurrentExecution
                    ? Job.FromExpression(() =>
                        jobProcessor.CallExternalApiWithConcurrencyControl(request.ApiEndpoint, request.Payload))
                    : Job.FromExpression(() => jobProcessor.CallExternalApi(request.ApiEndpoint, request.Payload)),
                new EnqueuedState());

        return Results.Ok(new { JobId = jobId });
    });


    app.MapPost("/jobs/schedule/future", async (JobFutureRequest request, IBackgroundJobClient backgroundJobClient) =>
    {
        string jobId;
        if (!TimeZoneValidator.IsValidTimeZone(request.TimeZoneId)) return Results.BadRequest("Invalid timezone");
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(request.TimeZoneId); // e.g., "Central Standard Time"

        var scheduledTimespan = TimeZoneUtility.ConvertToDateTimeOffset(request.ScheduledDateTime, timeZone);

        if (request.ScheduledDateTime.ToUniversalTime() <= DateTime.UtcNow)
        {
            // If the scheduled time is in the past or now, enqueue immediately
            jobId = backgroundJobClient.Create(
                request.PreventConcurrentExecution
                    ? Job.FromExpression(() =>
                        jobProcessor.CallExternalApiWithConcurrencyControl(request.ApiEndpoint, request.Payload))
                    : Job.FromExpression(() => jobProcessor.CallExternalApi(request.ApiEndpoint, request.Payload)),
                new EnqueuedState());
            JobStorage.Current.GetConnection()
                .SetJobParameter(jobId, "DisplayName", new Uri(request.ApiEndpoint).Segments.Last().TrimEnd('/'));
        }
        else
        {
            // Schedule the job to run at the specified future time
            jobId = backgroundJobClient.Schedule(
                request.PreventConcurrentExecution
                    ? () => jobProcessor.CallExternalApiWithConcurrencyControl(request.ApiEndpoint, request.Payload)
                    : () => jobProcessor.CallExternalApi(request.ApiEndpoint, request.Payload),
                scheduledTimespan);
            JobStorage.Current.GetConnection()
                .SetJobParameter(jobId, "DisplayName", new Uri(request.ApiEndpoint).Segments.Last().TrimEnd('/'));
        }


        return Results.Ok(new { JobId = jobId });
    });


    _ = app.MapPost("/jobs/schedule/recurring", (RecurringJobRequest request) =>
    {
        TimeZoneInfo.TryFindSystemTimeZoneById(request.TimeZoneId ?? string.Empty, out var timeZone);

        if (timeZone is null)
        {
            Log.Warning($"Invalid Time Zone identifier provided {request.TimeZoneId}. Defaulting to UTC timezone");
            timeZone = TimeZoneInfo.Utc;
        }

        if (HangfireCronValidator.IsValidCronExpression(request.CronExpression))
        {
            var invalidCronMessage = $"Invalid Cron Expression {request.CronExpression}";
            Log.Error(invalidCronMessage);
            return Results.BadRequest(invalidCronMessage);
        }

        var recurringJobOptions = new RecurringJobOptions
        {
            TimeZone = timeZone
        };

        // Schedule the recurring job
        if (request.PreventConcurrentExecution)
            RecurringJob.AddOrUpdate(
                request.JobId,
                () => jobProcessor.CallExternalApiWithConcurrencyControl(request.ApiEndpoint, request.Payload),
                request.CronExpression,
                recurringJobOptions);
        else
            RecurringJob.AddOrUpdate(
                request.JobId,
                () => jobProcessor.CallExternalApi(request.ApiEndpoint, request.Payload),
                request.CronExpression,
                recurringJobOptions);

        return Results.Ok(
            $"Recurring job '{request.JobId}' scheduled or updated to run according to the new schedule: {request.CronExpression}.");
    });
}


app.MapDelete("/jobs/scheduled/{jobId}", (string jobId) =>
{
    if (string.IsNullOrWhiteSpace(jobId)) return Results.BadRequest("JobId must be provided.");

    using var connection = JobStorage.Current.GetConnection();
    // Get all recurring jobs
    var jobData = connection.GetJobData(jobId);

    if (jobData == null) return Results.NotFound($"Job with Id: '{jobId}' does not exist.");

    BackgroundJob.Delete(jobId);

    return Results.Ok($"Job with Id: '{jobId}' has been removed.");
});


app.MapDelete("/jobs/recurring/{jobId}", (string jobId) =>
{
    if (string.IsNullOrWhiteSpace(jobId)) return Results.BadRequest("JobId must be provided.");

    using var connection = JobStorage.Current.GetConnection();
    // Get all recurring jobs
    var recurringJobs = connection.GetRecurringJobs();
    var recurringJob = recurringJobs.FirstOrDefault(j => j.Id == jobId);

    if (recurringJob == null) return Results.NotFound($"Recurring job '{jobId}' does not exist.");

    // Remove the recurring job
    RecurringJob.RemoveIfExists(jobId);

    return Results.Ok($"Recurring job '{jobId}' has been removed.");
});


app.UseHttpsRedirection();

app.UseStaticFiles();
try
{
// Endpoint configuration
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers(); // Map API controllers
    });
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host for HangScheduler terminated unexpectedly");
}

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host for HangScheduler terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}