namespace InventoryManagementSystem.Application.Common.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    string? IpAddress { get; }
    int? CompanyId { get; }
    int? SupplierId { get; }
    bool IsSuperAdmin { get; }
}
