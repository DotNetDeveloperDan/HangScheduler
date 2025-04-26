using Hangfire.Common;
using Hangfire.Server;

namespace HangScheduler.Api.Filters
{
    public class PreventDuplicateJobFilter(int timeoutInSeconds) : JobFilterAttribute, IServerFilter
    {
        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(timeoutInSeconds);

        public void OnPerforming(PerformingContext context)
        {
            if (!context.GetJobParameter<bool>("PreventConcurrentExecution"))
            {
                return;
            }

            var jobKey = GetJobKey(context);

            var distributedLock = context.Connection.AcquireDistributedLock(jobKey, _timeout);

            context.Items["DistributedLock"] = distributedLock;
        }

        public void OnPerformed(PerformedContext context)
        {
            if (context.Items.TryGetValue("DistributedLock", out var lockObj))
            {
                ((IDisposable)lockObj).Dispose();
            }
        }

        private string GetJobKey(PerformingContext context)
        {
            // Generate a unique key based on job type and arguments
            var job = context.BackgroundJob.Job;
            return $"{job.Type.FullName}.{job.Method.Name}.{string.Join(".", job.Args)}";
        }
    }
}
