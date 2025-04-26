using NCrontab;

namespace HangScheduler.Api.Utilities
{
    public static class HangfireCronValidator
    {
        public static bool IsValidCronExpression(string cronExpression)
        {
            try
            {
                // Attempt to parse the Hangfire cron expression
                CrontabSchedule.Parse(cronExpression);
                return true; // The expression is valid
            }
            catch (CrontabException)
            {
                // Invalid cron expression
                return false;
            }
            catch (Exception)
            {
                // Handle other errors
                return false;
            }
        }
    }
}
