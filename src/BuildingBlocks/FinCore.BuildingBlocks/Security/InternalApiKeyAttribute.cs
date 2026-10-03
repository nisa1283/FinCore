using FinCore.BuildingBlocks.Exceptions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinCore.BuildingBlocks.Security;

public class InternalApiKeyAttribute : Attribute, IAsyncActionFilter
{
    public const string HeaderName = "X-Internal-Api-Key";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var expectedKey = config["InternalApi:Key"];

        var hasHeader = context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var providedKey);

        if (string.IsNullOrWhiteSpace(expectedKey) || !hasHeader || providedKey.ToString() != expectedKey)
            throw new UnauthorizedAppException("Invalid internal API key.");

        await next();
    }
}