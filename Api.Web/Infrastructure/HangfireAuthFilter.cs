using Api.Domain.Constants;
using Hangfire.Dashboard;

namespace Api.Web.Infrastructure;

public class HangfireAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var isAdministrator = httpContext.User.IsInRole(Roles.Administrator);

        return httpContext.User.Identity?.IsAuthenticated != null && isAdministrator;
    }
}
