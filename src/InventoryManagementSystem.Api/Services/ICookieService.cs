using System;

namespace InventoryManagementSystem.Api.Services;

public interface ICookieService
{
    void SetAuthCookies(string accessToken, string refreshToken, DateTime accessTokenExpiry, DateTime refreshTokenExpiry, bool rememberMe);
    void ClearAuthCookies();
    string? GetRefreshToken();
    string? GetAccessToken();
}
