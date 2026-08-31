using Microsoft.AspNetCore.Http;
using System;

namespace InventoryManagementSystem.Api.Services;

public class CookieService : ICookieService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public const string AccessTokenCookieName = "access_token";
    public const string RefreshTokenCookieName = "refresh_token";

    public CookieService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private HttpContext Context => _httpContextAccessor.HttpContext 
        ?? throw new InvalidOperationException("HttpContext is not available.");

    public void SetAuthCookies(string accessToken, string refreshToken, DateTime accessTokenExpiry, DateTime refreshTokenExpiry, bool rememberMe)
    {
        // For cross-origin requests (e.g. Angular on localhost:4200 calling API on localhost:7152), 
        // SameSite must be None and Secure must be true.
        bool isHttps = Context.Request.IsHttps;

        // Access Token Cookie (HttpOnly)
        // Set cookie lifetime to match refresh token lifetime so the expired JWT is retained
        // and passed to /api/Auth/refresh-token to extract user identity.
        CookieOptions accessCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps,
            SameSite = isHttps ? SameSiteMode.None : SameSiteMode.Lax,
            Expires = rememberMe ? refreshTokenExpiry : null,
            Path = "/"
        };
        Context.Response.Cookies.Append(AccessTokenCookieName, accessToken, accessCookieOptions);

        // Refresh Token Cookie (HttpOnly)
        CookieOptions refreshCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps,
            SameSite = isHttps ? SameSiteMode.None : SameSiteMode.Lax,
            Expires = rememberMe ? refreshTokenExpiry : null,
            Path = "/"
        };
        Context.Response.Cookies.Append(RefreshTokenCookieName, refreshToken, refreshCookieOptions);
    }

    public void ClearAuthCookies()
    {
        bool isHttps = Context.Request.IsHttps;

        CookieOptions deleteOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps,
            SameSite = isHttps ? SameSiteMode.None : SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddDays(-1),
            Path = "/"
        };

        CookieOptions clientDeleteOptions = new CookieOptions
        {
            HttpOnly = false,
            Secure = isHttps,
            SameSite = isHttps ? SameSiteMode.None : SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddDays(-1),
            Path = "/"
        };

        Context.Response.Cookies.Delete(AccessTokenCookieName, deleteOptions);
        Context.Response.Cookies.Delete(RefreshTokenCookieName, deleteOptions);
        Context.Response.Cookies.Delete("XSRF-TOKEN", clientDeleteOptions);
    }

    public string? GetRefreshToken()
    {
        Context.Request.Cookies.TryGetValue(RefreshTokenCookieName, out var token);
        return token;
    }

    public string? GetAccessToken()
    {
        Context.Request.Cookies.TryGetValue(AccessTokenCookieName, out var token);
        return token;
    }
}
