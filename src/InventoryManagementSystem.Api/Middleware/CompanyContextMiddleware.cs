using InventoryManagementSystem.Application.Common.Models;

namespace InventoryManagementSystem.Api.Middleware;

public class CompanyContextMiddleware
{
    private readonly RequestDelegate _next;

    public CompanyContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && (context.Request.Path.StartsWithSegments("/api/master") || context.Request.Path.StartsWithSegments("/api/process")
                || context.Request.Path.StartsWithSegments("/api/usermanage/supplier-accounts")
                || context.Request.Path.StartsWithSegments("/api/supplier"))
            && context.Request.Headers.TryGetValue("X-Company-Id", out var expectedCompany)
            && expectedCompany.ToString() != context.User.FindFirst("company_id")?.Value)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status409Conflict,
                Success = false,
                Message = "The company account changed. Reload the page before continuing."
            });
            return;
        }
        if (context.User.Identity?.IsAuthenticated == true
            && context.Request.Path.StartsWithSegments("/api/supplier")
            && context.Request.Headers.TryGetValue("X-Supplier-Id", out var expectedSupplier)
            && expectedSupplier.ToString() != context.User.FindFirst("supplier_id")?.Value)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status409Conflict,
                Success = false,
                Message = "The supplier account changed. Reload the page before continuing."
            });
            return;
        }
        await _next(context);
    }
}
