using InventoryManagementSystem.Application.Auth.Models;
using InventoryManagementSystem.Application.UserManage.SupplierAccounts.DTOs;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<Dictionary<string, string>> GetUserNamesAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default);
    Task<AuthResultDto> RegisterAsync(RegisterRequest request);
    Task<AuthResultDto> LoginAsync(LoginRequest request);
    Task<AuthResultDto> SelectCompanyAsync(string userId, int companyId);
    Task<AuthResultDto> RefreshTokenAsync(RefreshTokenRequest request);
    Task<string?> GeneratePasswordResetTokenAsync(string email);
    Task<ResultDto> ResetPasswordAsync(ResetPasswordRequest request);
    Task<ResultDto> ChangePasswordAsync(string userId, ChangePasswordRequest request);
    Task<AuthResultDto> RegisterCompanyAdminAsync(string userName, string email, string password, int companyId, string companyName, string role);
    Task<ResultDto> CreateSupplierAccountAsync(string userName, string email, string password, int companyId, int supplierId);
    Task<List<SupplierAccountDto>> GetSupplierAccountsAsync(int supplierId, int companyId, CancellationToken cancellationToken = default);
    Task<ResultDto> DeleteSupplierAccountAsync(string userId, int companyId);
}
