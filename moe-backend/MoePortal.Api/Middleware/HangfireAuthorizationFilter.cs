using Hangfire.Dashboard;
using System.Diagnostics.CodeAnalysis;

namespace MoePortal.Api.Middleware;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize([NotNull] DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        // Allow all local requests
        return httpContext.Request.IsLocal();
    }
}

public static class HttpRequestExtensions
{
    public static bool IsLocal(this HttpRequest req)
    {
        var connection = req.HttpContext.Connection;
        if (connection.RemoteIpAddress != null)
        {
            if (connection.LocalIpAddress != null)
            {
                return connection.RemoteIpAddress.Equals(connection.LocalIpAddress);
            }
            else
            {
                return System.Net.IPAddress.IsLoopback(connection.RemoteIpAddress);
            }
        }
        
        // for in memory TestServer or when dealing with default connection info
        if (connection.RemoteIpAddress == null && connection.LocalIpAddress == null)
        {
            return true;
        }

        return false;
    }
}
