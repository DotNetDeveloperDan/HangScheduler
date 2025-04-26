namespace HangScheduler.Api.Models
{
    public class RecurringJobRequest:BaseJobRequest
    {
        public string JobId { get; set; }
        public string? Payload { get; set; } = string.Empty;
        public string CronExpression { get; set; } // Cron expression to specify job frequency
        public string? TimeZoneId { get; set; }
        public string ContinueAfterJobId { get; set; }

    }
}
