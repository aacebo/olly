using System.Net;

namespace OS.Agent.Api.Middleware;

public class ErrorMiddleware(ILogger logger) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError("{}", ex);

            await Results.Json(new
            {
                code = HttpStatusCode.InternalServerError,
                message = ex.Message
            }).ExecuteAsync(context);
        }
    }
}