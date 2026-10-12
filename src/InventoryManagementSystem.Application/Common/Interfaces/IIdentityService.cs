using InventoryManagementSystem.Application.Auth.Models;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<Dictionary<string, string>> GetUserNamesAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default);
    Task<AuthResultDto> RegisterAsync(RegisterRequest request);
    Task<AuthResultDto> LoginAsync(LoginRequest request);
    Task<AuthResultDto> RefreshTokenAsync(RefreshTokenRequest request);
    Task<string?> GeneratePasswordResetTokenAsync(string email);
    Task<ResultDto> ResetPasswordAsync(ResetPasswordRequest request);
    Task<ResultDto> ChangePasswordAsync(string userId, ChangePasswordRequest request);
    Task<AuthResultDto> RegisterCompanyAdminAsync(string userName, string email, string password, int companyId, string companyName, string role);
}
