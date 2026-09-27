using System.Security.Claims;
using InventoryManagementSystem.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace InventoryManagementSystem.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub")
                             ?? _httpContextAccessor.HttpContext?.User?.Identity?.Name;

    public string? UserName => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name)
                               ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("unique_name")
                               ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("name")
                               ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("username")
                               ?? _httpContextAccessor.HttpContext?.User?.Identity?.Name;

    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

    public int? CompanyId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("company_id")
                        ?? _httpContextAccessor.HttpContext?.User?.FindFirst("CompanyId");
            if (claim != null && int.TryParse(claim.Value, out var companyId))
            {
                return companyId;
            }
            return null;
        }
    }

    public bool IsSuperAdmin
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity?.IsAuthenticated == true)
            {
                return false;
            }
            return user.IsInRole("SuperAdmin") || user.IsInRole("Administrator");
        }
    }
}
