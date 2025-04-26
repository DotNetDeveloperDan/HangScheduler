using System.Diagnostics;
using System.Text;
using Hangfire;
using HangScheduler.Api.Interfaces;

namespace HangScheduler.Api;

public class JobProcessor(ILogger<JobProcessor> logger, IHttpClientFactory httpClientFactory)
    : IJobProcessor
{
    
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("JobProcessorClient");

    // Method to call an external API
    /// <summary>
    /// Calls the external API.
    /// </summary>
    /// <param name="apiEndpoint">The API endpoint.</param>
    /// <param name="jsonPayload">The json payload.</param>
    [AutomaticRetry(Attempts = 3)]
    public async Task CallExternalApi(string apiEndpoint, string? jsonPayload = null)
    {
        await ProcessApiCall(apiEndpoint, jsonPayload);
    }

    [AutomaticRetry(Attempts = 0)]
    [DisableConcurrentExecution(timeoutInSeconds: 30)]
    public async Task CallExternalApiWithConcurrencyControl(string apiEndpoint, string? jsonPayload = null)
    {
        await ProcessApiCall(apiEndpoint, jsonPayload);
    }

    private async Task ProcessApiCall(string apiEndpoint, string? jsonPayload = null)
    {
        logger.LogInformation("Calling external API.");
        
        HttpResponseMessage response;

        if (string.IsNullOrWhiteSpace(jsonPayload))
        {
            logger.LogInformation($"Running a POST to {apiEndpoint}");
            var stopwatch = Stopwatch.StartNew();
            response = await _httpClient.PostAsync(apiEndpoint,null);
            stopwatch.Stop();
            var elapsedTime = stopwatch.ElapsedMilliseconds;
            logger.LogInformation($"Request took {elapsedTime} ms");
        }
        else
        {
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            response = await _httpClient.PostAsync(apiEndpoint, content);
        }

        response.EnsureSuccessStatusCode();
    }



}