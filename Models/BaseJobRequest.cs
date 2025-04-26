namespace HangScheduler.Api.Models
{
    public abstract class BaseJobRequest
    {
       
        public string ApiEndpoint { get; set; }
        public bool PreventConcurrentExecution { get; set; }
       
    }
}
