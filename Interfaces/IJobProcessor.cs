namespace HangScheduler.Api.Interfaces;

public interface IJobProcessor
{
    public Task CallExternalApi(string apiEndpoint, string? jsonPayload = null);
    public Task CallExternalApiWithConcurrencyControl(string apiEndpoint, string? jsonPayload = null);
    public Task CallExternalApiMaxRetry(string apiEndpoint, string? jsonPayload = null);
    public Task CallExternalApiWithConcurrencyControlMaxRetry(string apiEndpoint, string? jsonPayload = null);
}