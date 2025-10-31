using System.Text.Json;

using Olly.Errors;

namespace Olly.Api.Middleware;

public class ErrorMiddleware(ILogger logger, JsonSerializerOptions? jsonOptions) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (HttpException ex)
        {
            logger.LogWarning("{}", ex);
            await Results.Json(ex, jsonOptions, statusCode: (int)ex.Code).ExecuteAsync(context);
        }
        catch (Exception ex)
        {
            logger.LogError("{}", ex);
            await Results.Json(new HttpException(ex.Message, ex)).ExecuteAsync(context);
        }
    }
}