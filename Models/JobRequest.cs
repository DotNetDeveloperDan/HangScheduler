namespace HangScheduler.Api.Models
{
    public class JobRequest:BaseJobRequest
    {
        public string? Payload { get; set; } = string.Empty;
        public string ContinueAfterJobId { get; set; }
    }

}
