using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IdentityService> _logger;

    public IdentityService(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        ILogger<IdentityService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthResultDto> RegisterAsync(RegisterRequest request)
    {
        _logger.LogInformation("Registration attempt for user: {UserName}, Email: {Email}", request.UserName, request.Email);

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return new AuthResultDto
            {
                Succeeded = false,
                Message = "User already exists with this email.",
                Errors = new List<string> { "Email is already registered." }
            };
        }

        var user = new IdentityUser
        {
            UserName = request.UserName,
            Email = request.Email
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return new AuthResultDto
            {
                Succeeded = false,
                Message = "User registration failed.",
                Errors = result.Errors.Select(e => e.Description).ToList()
            };
        }

        string roleName = string.IsNullOrWhiteSpace(request.Role) ? "User" : request.Role;
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            await _roleManager.CreateAsync(new IdentityRole(roleName));
        }

        await _userManager.AddToRoleAsync(user, roleName);

        _logger.LogInformation("User registered successfully: {Email} with role {Role}", user.Email, roleName);

        return await GenerateAuthResponseAsync(user);
    }

    public async Task<AuthResultDto> LoginAsync(LoginRequest request)
    {
        _logger.LogInformation("Login attempt for identifier: {Identifier}", request.UserNameOrEmail);

        IdentityUser? user = request.UserNameOrEmail.Contains("@")
            ? await _userManager.FindByEmailAsync(request.UserNameOrEmail)
            : await _userManager.FindByNameAsync(request.UserNameOrEmail);

        if (user == null)
        {
            return new AuthResultDto
            {
                Succeeded = false,
                Message = "Invalid credentials.",
                Errors = new List<string> { "User not found." }
            };
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
            {
                return new AuthResultDto
                {
                    Succeeded = false,
                    Message = "Account is locked out."
                };
            }

            return new AuthResultDto
            {
                Succeeded = false,
                Message = "Invalid credentials.",
                Errors = new List<string> { "Incorrect password." }
            };
        }

        _logger.LogInformation("Login successful for user: {Email}", user.Email);
        return await GenerateAuthResponseAsync(user);
    }

    public async Task<AuthResultDto> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var principal = GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal == null)
        {
            return new AuthResultDto
            {
                Succeeded = false,
                Message = "Invalid access token."
            };
        }

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return new AuthResultDto
            {
                Succeeded = false,
                Message = "Invalid token claims."
            };
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return new AuthResultDto
            {
                Succeeded = false,
                Message = "User not found."
            };
        }

        var savedRefreshToken = await _userManager.GetAuthenticationTokenAsync(user, "InventoryManagementSystem", "RefreshToken");
        var savedExpiryString = await _userManager.GetAuthenticationTokenAsync(user, "InventoryManagementSystem", "RefreshTokenExpiry");

        if (savedRefreshToken != request.RefreshToken)
        {
            return new AuthResultDto
            {
                Succeeded = false,
                Message = "Invalid refresh token."
            };
        }

        if (DateTime.TryParse(savedExpiryString, out var expiry) && expiry < DateTime.UtcNow)
        {
            return new AuthResultDto
            {
                Succeeded = false,
                Message = "Refresh token has expired. Please login again."
            };
        }

        _logger.LogInformation("Token refreshed successfully for user: {Email}", user.Email);
        return await GenerateAuthResponseAsync(user);
    }

    public async Task<string?> GeneratePasswordResetTokenAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return null;

        return await _userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task<ResultDto> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return ResultDto.Failure("User not found.");
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            return ResultDto.Failure(result.Errors.Select(e => e.Description), "Password reset failed.");
        }

        return ResultDto.Success("Password reset successfully.");
    }

    public async Task<ResultDto> ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return ResultDto.Failure("User not found.");
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            return ResultDto.Failure(result.Errors.Select(e => e.Description), "Change password failed.");
        }

        return ResultDto.Success("Password changed successfully.");
    }

    private async Task<AuthResultDto> GenerateAuthResponseAsync(IdentityUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var secretKey = _configuration["JwtSettings:SecretKey"] ?? "SuperSecretKeyForInventoryManagementSystem_JwtToken_2026!#";
        var issuer = _configuration["JwtSettings:Issuer"] ?? "InventoryManagementSystem";
        var audience = _configuration["JwtSettings:Audience"] ?? "InventoryManagementSystemClient";
        var expiryMinutes = int.TryParse(_configuration["JwtSettings:ExpiryMinutes"], out var exp) ? exp : 60;
        var refreshTokenExpiryDays = int.TryParse(_configuration["JwtSettings:RefreshTokenExpiryDays"], out var refExp) ? refExp : 7;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var accessToken = tokenHandler.WriteToken(token);

        var refreshToken = GenerateRefreshToken();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(refreshTokenExpiryDays);

        await _userManager.SetAuthenticationTokenAsync(user, "InventoryManagementSystem", "RefreshToken", refreshToken);
        await _userManager.SetAuthenticationTokenAsync(user, "InventoryManagementSystem", "RefreshTokenExpiry", refreshTokenExpiry.ToString("O"));

        return new AuthResultDto
        {
            Succeeded = true,
            Message = "Authentication successful.",
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            Expiry = expires,
            UserId = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            Roles = roles.ToList()
        };
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var secretKey = _configuration["JwtSettings:SecretKey"] ?? "SuperSecretKeyForInventoryManagementSystem_JwtToken_2026!#";
        var issuer = _configuration["JwtSettings:Issuer"] ?? "InventoryManagementSystem";
        var audience = _configuration["JwtSettings:Audience"] ?? "InventoryManagementSystemClient";

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            return null;
        }

        return principal;
    }
}
