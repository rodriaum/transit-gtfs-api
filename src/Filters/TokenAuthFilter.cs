using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using Serilog;

namespace Tranzor.Filters;

public class TokenAuthFilter : IAsyncActionFilter
{
    private readonly string? _apiToken;

    public TokenAuthFilter()
    {
        string? apiToken = Environment.GetEnvironmentVariable("API_TOKEN");

        if (string.IsNullOrEmpty(apiToken))
        {
            Log.Warning("No API key was assigned (API_TOKEN). The system will refuse all requests.");
        }

        _apiToken = apiToken;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (string.IsNullOrEmpty(_apiToken))
        {
            context.Result = new UnauthorizedObjectResult(new { message = "No API key was assigned. The system will refuse all requests." });
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue("X-API-KEY", out StringValues apiKeyHeader))
        {
            context.Result = new UnauthorizedObjectResult(new { message = "X-API-KEY header is missing" });
            return;
        }

        string token = apiKeyHeader.ToString();

        if (string.IsNullOrEmpty(token) || token != _apiToken)
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Invalid API key" });
            return;
        }

        await next();
    }
}