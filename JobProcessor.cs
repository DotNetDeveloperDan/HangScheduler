using System.Text;
using Hangfire;
using HangScheduler.Api.Interfaces;

namespace HangScheduler.Api;

public class JobProcessor : IJobProcessor
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<JobProcessor> _logger;

    public JobProcessor(ILogger<JobProcessor> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("JobProcessorClient");
    }

    // Method to call an external API
    /// <summary>
    ///     Calls the external API.
    /// </summary>
    /// <param name="apiEndpoint">The API endpoint.</param>
    /// <param name="jsonPayload">The json payload.</param>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [20])]
    public async Task CallExternalApi(string apiEndpoint, string? jsonPayload = null)
    {
        await ProcessApiCall(apiEndpoint, jsonPayload);
    }


    /// <summary>
    ///     Calls the external API maximum retry.
    /// </summary>
    /// <param name="apiEndpoint">The API endpoint.</param>
    /// <param name="jsonPayload">The json payload.</param>
    [AutomaticRetry(Attempts = int.MaxValue, DelaysInSeconds = [60])]
    public async Task CallExternalApiMaxRetry(string apiEndpoint, string? jsonPayload = null)
    {
        await ProcessApiCall(apiEndpoint, jsonPayload);
    }

    /// <summary>
    ///     Calls the external API with concurrency control.
    /// </summary>
    /// <param name="apiEndpoint">The API endpoint.</param>
    /// <param name="jsonPayload">The json payload.</param>
    [AutomaticRetry(Attempts = 0)]
    [DisableConcurrentExecution(30)]
    public async Task CallExternalApiWithConcurrencyControl(string apiEndpoint, string? jsonPayload = null)
    {
        await ProcessApiCall(apiEndpoint, jsonPayload);
    }


    /// <summary>
    ///     Calls the external API with concurrency control maximum retry.
    /// </summary>
    /// <param name="apiEndpoint">The API endpoint.</param>
    /// <param name="jsonPayload">The json payload.</param>
    [AutomaticRetry(Attempts = int.MaxValue, DelaysInSeconds = [60])]
    [DisableConcurrentExecution(30)]
    public async Task CallExternalApiWithConcurrencyControlMaxRetry(string apiEndpoint, string? jsonPayload = null)
    {
        await ProcessApiCall(apiEndpoint, jsonPayload);
    }


    private async Task ProcessApiCall(string apiEndpoint, string? jsonPayload = null)
    {
        _logger.LogInformation("Calling external API.");

        HttpResponseMessage response;

        // If no payload is provided, send an empty JSON object to ensure the Content-Type is set.
        if (string.IsNullOrWhiteSpace(jsonPayload))
        {
            _logger.LogInformation($"Running a POST to {apiEndpoint} with empty payload.");
            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            response = await _httpClient.PostAsync(apiEndpoint, content);
        }
        else
        {
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            response = await _httpClient.PostAsync(apiEndpoint, content);
        }

        response.EnsureSuccessStatusCode();
    }
}