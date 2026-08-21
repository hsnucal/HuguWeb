using Microsoft.AspNetCore.Antiforgery;

namespace HuGuWeb.Api.Endpoints;

internal sealed class ValidateAntiforgeryFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var antiforgery = context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
        await antiforgery.ValidateRequestAsync(context.HttpContext);
        return await next(context);
    }
}
