using Hangfire.Dashboard;

namespace HangScheduler.Api.Filters
{
    public class AllowAllDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            return true; // Allows all users to access the dashboard
        }
    }

}
