using Olly.Contexts;

namespace Olly.Api.Middleware;

public class ContextMiddleware(IHttpContextAccessor httpContextAccessor, IOllyContextAccessor ollyContextAccessor) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        ollyContextAccessor.Context.TraceId = httpContextAccessor.HttpContext!.TraceIdentifier;
        ollyContextAccessor.Context.Provider = httpContextAccessor.HttpContext!.RequestServices;
        await next(context);
    }
}