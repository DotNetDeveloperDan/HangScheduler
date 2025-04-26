namespace HangScheduler.Api.Models
{
    public class JobFutureRequest: BaseJobRequest
    {
        public string? Payload { get; set; } = string.Empty;
        public DateTime ScheduledDateTime { get; set; }
        public string TimeZoneId { get; set; } = "Central Standard Time";
    }
}
