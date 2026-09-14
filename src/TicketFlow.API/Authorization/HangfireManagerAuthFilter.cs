using Hangfire.Dashboard;

namespace TicketFlow.API.Authorization;

public class HangfireManagerAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.IsInRole("Manager");
    }
}