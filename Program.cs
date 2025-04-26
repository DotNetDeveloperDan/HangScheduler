using System.Reflection;
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

// Create the web application builder
var builder = WebApplication.CreateBuilder(args);

// *** Configuration and Logging Setup ***
builder.Configuration
    .AddJsonFile("appsettings.json", false, true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, true)
    .AddEnvironmentVariables();

// Configure Serilog from appsettings.json
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host
    .UseSerilog()
    .UseSystemd();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// *** Service Registration ***
// Add controller support for API endpoints
builder.Services.AddControllers();

// Configure Hangfire with LiteDB storage
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseLiteDbStorage("Filename=hangfire_lite.db;Mode=Exclusive;"));
builder.Services.AddHangfireServer();

// Register job processor and HTTP client
builder.Services.AddScoped<IJobProcessor, JobProcessor>();
builder.Services.AddHttpClient("JobProcessorClient")
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();
        if (builder.Environment.IsDevelopment())
        {
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            Log.Information("SSL validation bypassed in development.");
        }

        return handler;
    });

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(settings =>
{
    settings.Title = builder.Environment.ApplicationName;
    settings.Version = Assembly.GetEntryAssembly()?
                           .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                           .InformationalVersion.Split('+', 2)[0]
                       ?? "1.0.0";
});

// *** Build and Configure the Application ***
var app = builder.Build();

// Middleware configuration
app.UseMiddleware<GlobalExceptionCatcher>();
app.UseRouting();
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new AllowAllDashboardAuthorizationFilter()]
});

// Enable Swagger in non-production environments
if (!app.Environment.IsProduction())
{
    app.UseOpenApi();
    app.UseSwaggerUi(ui =>
    {
        ui.DocExpansion = "list";
        ui.TagsSorter = "alpha";
        ui.OperationsSorter = "alpha";
    });
}

// Additional middleware
app.UseHttpsRedirection();
app.UseStaticFiles();

// *** API Endpoint Definitions ***
using (var scope = app.Services.CreateScope())
{
    var jobProcessor = scope.ServiceProvider.GetRequiredService<IJobProcessor>();

    // Schedule a job to run immediately
    app.MapPost("/jobs/schedule/now", async (JobRequest request, IBackgroundJobClient backgroundJobClient) =>
    {
        var jobId = !string.IsNullOrEmpty(request.ContinueAfterJobId)
            ? backgroundJobClient.ContinueJobWith(
                request.ContinueAfterJobId,
                () => request.PreventConcurrentExecution
                    ? jobProcessor.CallExternalApiWithConcurrencyControl(request.ApiEndpoint, request.Payload)
                    : jobProcessor.CallExternalApi(request.ApiEndpoint, request.Payload))
            : backgroundJobClient.Create(
                request.PreventConcurrentExecution
                    ? Job.FromExpression(() =>
                        jobProcessor.CallExternalApiWithConcurrencyControl(request.ApiEndpoint, request.Payload))
                    : Job.FromExpression(() => jobProcessor.CallExternalApi(request.ApiEndpoint, request.Payload)),
                new EnqueuedState());

        Log.Information("Job scheduled to run now with ID: {JobId}", jobId);
        return Results.Ok(new { JobId = jobId });
    });

    // Schedule a job to run immediately with maximum retries
    app.MapPost("/jobs/schedule/now/max_retry", async (JobRequest request, IBackgroundJobClient backgroundJobClient) =>
    {
        var jobId = !string.IsNullOrEmpty(request.ContinueAfterJobId)
            ? backgroundJobClient.ContinueJobWith(
                request.ContinueAfterJobId,
                () => request.PreventConcurrentExecution
                    ? jobProcessor.CallExternalApiWithConcurrencyControlMaxRetry(request.ApiEndpoint, request.Payload)
                    : jobProcessor.CallExternalApiMaxRetry(request.ApiEndpoint, request.Payload))
            : backgroundJobClient.Create(
                request.PreventConcurrentExecution
                    ? Job.FromExpression(() =>
                        jobProcessor.CallExternalApiWithConcurrencyControlMaxRetry(request.ApiEndpoint,
                            request.Payload))
                    : Job.FromExpression(() =>
                        jobProcessor.CallExternalApiMaxRetry(request.ApiEndpoint, request.Payload)),
                new EnqueuedState());

        Log.Information("Job scheduled to run now with max retries, ID: {JobId}", jobId);
        return Results.Ok(new { JobId = jobId });
    });

    // Schedule a job to run in the future
    app.MapPost("/jobs/schedule/future", async (JobFutureRequest request, IBackgroundJobClient backgroundJobClient) =>
    {
        if (!TimeZoneValidator.IsValidTimeZone(request.TimeZoneId))
            return Results.BadRequest("Invalid timezone");

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(request.TimeZoneId);
        var scheduledTimespan = TimeZoneUtility.ConvertToDateTimeOffset(request.ScheduledDateTime, timeZone);

        var jobId = request.ScheduledDateTime.ToUniversalTime() <= DateTime.UtcNow
            ? backgroundJobClient.Create(
                request.PreventConcurrentExecution
                    ? Job.FromExpression(() =>
                        jobProcessor.CallExternalApiWithConcurrencyControl(request.ApiEndpoint, request.Payload))
                    : Job.FromExpression(() => jobProcessor.CallExternalApi(request.ApiEndpoint, request.Payload)),
                new EnqueuedState())
            : backgroundJobClient.Schedule(
                request.PreventConcurrentExecution
                    ? () => jobProcessor.CallExternalApiWithConcurrencyControl(request.ApiEndpoint, request.Payload)
                    : () => jobProcessor.CallExternalApi(request.ApiEndpoint, request.Payload),
                scheduledTimespan);

        JobStorage.Current.GetConnection()
            .SetJobParameter(jobId, "DisplayName", new Uri(request.ApiEndpoint).Segments.Last().TrimEnd('/'));
        Log.Information("Job scheduled for future execution with ID: {JobId}", jobId);
        return Results.Ok(new { JobId = jobId });
    });

    // Schedule a job to run in the future with maximum retries
    app.MapPost("/jobs/schedule/future/max_retry",
        async (JobFutureRequest request, IBackgroundJobClient backgroundJobClient) =>
        {
            if (!TimeZoneValidator.IsValidTimeZone(request.TimeZoneId))
                return Results.BadRequest("Invalid timezone");

            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(request.TimeZoneId);
            var scheduledTimespan = TimeZoneUtility.ConvertToDateTimeOffset(request.ScheduledDateTime, timeZone);

            var jobId = request.ScheduledDateTime.ToUniversalTime() <= DateTime.UtcNow
                ? backgroundJobClient.Create(
                    request.PreventConcurrentExecution
                        ? Job.FromExpression(() =>
                            jobProcessor.CallExternalApiWithConcurrencyControlMaxRetry(request.ApiEndpoint,
                                request.Payload))
                        : Job.FromExpression(() =>
                            jobProcessor.CallExternalApiMaxRetry(request.ApiEndpoint, request.Payload)),
                    new EnqueuedState())
                : backgroundJobClient.Schedule(
                    request.PreventConcurrentExecution
                        ? () => jobProcessor.CallExternalApiWithConcurrencyControlMaxRetry(request.ApiEndpoint,
                            request.Payload)
                        : () => jobProcessor.CallExternalApiMaxRetry(request.ApiEndpoint, request.Payload),
                    scheduledTimespan);

            JobStorage.Current.GetConnection()
                .SetJobParameter(jobId, "DisplayName", new Uri(request.ApiEndpoint).Segments.Last().TrimEnd('/'));
            Log.Information("Job scheduled for future execution with max retries, ID: {JobId}", jobId);
            return Results.Ok(new { JobId = jobId });
        });

    // Schedule a recurring job
    app.MapPost("/jobs/schedule/recurring", (RecurringJobRequest request) =>
    {
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(request.TimeZoneId ?? string.Empty, out var timeZone))
        {
            Log.Warning("Invalid Time Zone ID: {TimeZoneId}. Defaulting to UTC.", request.TimeZoneId);
            timeZone = TimeZoneInfo.Utc;
        }

        if (!HangfireCronValidator.IsValidCronExpression(request.CronExpression))
        {
            var message = $"Invalid Cron Expression: {request.CronExpression}";
            Log.Error(message);
            return Results.BadRequest(message);
        }

        var options = new RecurringJobOptions { TimeZone = timeZone };
        if (request.PreventConcurrentExecution)
            RecurringJob.AddOrUpdate(
                request.JobId,
                () => jobProcessor.CallExternalApiWithConcurrencyControl(request.ApiEndpoint, request.Payload),
                request.CronExpression,
                options);
        else
            RecurringJob.AddOrUpdate(
                request.JobId,
                () => jobProcessor.CallExternalApi(request.ApiEndpoint, request.Payload),
                request.CronExpression,
                options);

        Log.Information("Recurring job '{JobId}' scheduled with cron: {CronExpression}", request.JobId,
            request.CronExpression);
        return Results.Ok($"Recurring job '{request.JobId}' scheduled or updated with cron: {request.CronExpression}.");
    });

    // Schedule a recurring job with maximum retries
    app.MapPost("/jobs/schedule/recurring/max_retry", (RecurringJobRequest request) =>
    {
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(request.TimeZoneId ?? string.Empty, out var timeZone))
        {
            Log.Warning("Invalid Time Zone ID: {TimeZoneId}. Defaulting to UTC.", request.TimeZoneId);
            timeZone = TimeZoneInfo.Utc;
        }

        if (!HangfireCronValidator.IsValidCronExpression(request.CronExpression))
        {
            var message = $"Invalid Cron Expression: {request.CronExpression}";
            Log.Error(message);
            return Results.BadRequest(message);
        }

        var options = new RecurringJobOptions { TimeZone = timeZone };
        if (request.PreventConcurrentExecution)
            RecurringJob.AddOrUpdate(
                request.JobId,
                () => jobProcessor.CallExternalApiWithConcurrencyControlMaxRetry(request.ApiEndpoint, request.Payload),
                request.CronExpression,
                options);
        else
            RecurringJob.AddOrUpdate(
                request.JobId,
                () => jobProcessor.CallExternalApiMaxRetry(request.ApiEndpoint, request.Payload),
                request.CronExpression,
                options);

        Log.Information("Recurring job '{JobId}' scheduled with max retries and cron: {CronExpression}", request.JobId,
            request.CronExpression);
        return Results.Ok($"Recurring job '{request.JobId}' scheduled or updated with cron: {request.CronExpression}.");
    });

    // Delete a scheduled job
    app.MapDelete("/jobs/scheduled/{jobId}", (string jobId) =>
    {
        if (string.IsNullOrWhiteSpace(jobId))
            return Results.BadRequest("JobId must be provided.");

        using var connection = JobStorage.Current.GetConnection();
        if (connection.GetJobData(jobId) == null)
            return Results.NotFound($"Job with ID: '{jobId}' does not exist.");

        BackgroundJob.Delete(jobId);
        Log.Information("Scheduled job '{JobId}' has been deleted.", jobId);
        return Results.Ok($"Job with ID: '{jobId}' has been removed.");
    });

    // Delete a recurring job
    app.MapDelete("/jobs/recurring/{jobId}", (string jobId) =>
    {
        if (string.IsNullOrWhiteSpace(jobId))
            return Results.BadRequest("JobId must be provided.");

        using var connection = JobStorage.Current.GetConnection();
        var recurringJob = connection.GetRecurringJobs().FirstOrDefault(j => j.Id == jobId);
        if (recurringJob == null)
            return Results.NotFound($"Recurring job '{jobId}' does not exist.");

        RecurringJob.RemoveIfExists(jobId);
        Log.Information("Recurring job '{JobId}' has been deleted.", jobId);
        return Results.Ok($"Recurring job '{jobId}' has been removed.");
    });
}

// Map controller endpoints
app.UseEndpoints(endpoints => endpoints.MapControllers());

// *** Run the Application ***
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