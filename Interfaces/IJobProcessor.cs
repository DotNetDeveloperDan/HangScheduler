namespace HangScheduler.Api.Interfaces;

public interface IJobProcessor
{
    public Task CallExternalApi(string apiEndpoint, string? jsonPayload = null);
    public Task CallExternalApiWithConcurrencyControl(string apiEndpoint, string? jsonPayload = null);
}