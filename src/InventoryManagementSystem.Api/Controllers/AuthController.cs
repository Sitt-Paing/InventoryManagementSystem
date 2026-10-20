using InventoryManagementSystem.Application.Auth.Commands.SelectCompany;
using InventoryManagementSystem.Application.Auth.Queries.GetCompanyAccounts;
using InventoryManagementSystem.Application.Auth.Commands.ChangePassword;
using InventoryManagementSystem.Application.Auth.Commands.GenerateResetToken;
using InventoryManagementSystem.Application.Auth.Commands.LoginUser;
using InventoryManagementSystem.Application.Auth.Commands.RefreshToken;
using InventoryManagementSystem.Application.Auth.Commands.RegisterUser;
using InventoryManagementSystem.Application.Auth.Commands.ResetPassword;
using InventoryManagementSystem.Application.Auth.Models;
using InventoryManagementSystem.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

using InventoryManagementSystem.Api.Services;
using Microsoft.AspNetCore.Antiforgery;

namespace InventoryManagementSystem.Api.Controllers;

[Route("api/[controller]")]
public class AuthController : ApiControllerBase
{
    private readonly ICookieService _cookieService;
    private readonly IAntiforgery _antiforgery;

    public AuthController(ICookieService cookieService, IAntiforgery antiforgery)
    {
        _cookieService = cookieService;
        _antiforgery = antiforgery;
    }

    /// <summary>
    /// Register a new user account.
    /// </summary>
    [HttpPost("register")]
    [EndpointSummary("Register a new user account")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DefaultResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DefaultResponseModel), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command)
    {
        var result = await Mediator.Send(command);
        if (!result.Succeeded)
        {
            return BadRequest(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Success = false,
                Message = result.Message ?? "User registration failed.",
                Data = result
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = result.Message ?? "User registered successfully.",
            Data = result
        });
    }

    /// <summary>
    /// Authenticate a user by Username or Email, issue JWT tokens, and store in HttpOnly cookies.
    /// </summary>
    [HttpPost("login")]
    [EndpointSummary("Login account")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DefaultResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DefaultResponseModel), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand command)
    {
        var result = await Mediator.Send(command);
        if (!result.Succeeded)
        {
            return Unauthorized(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status401Unauthorized,
                Success = false,
                Message = result.Message ?? "Invalid credentials. Please check your username and password.",
                Data = result
            });
        }

        // Set HttpOnly authentication cookies
        DateTime accessExpiry = result.Expiry ?? DateTime.UtcNow.AddMinutes(15);
        DateTime refreshExpiry = DateTime.UtcNow.AddDays(15);
        _cookieService.SetAuthCookies(result.AccessToken ?? string.Empty, result.RefreshToken ?? string.Empty, accessExpiry, refreshExpiry, command.RememberMe);

        // Set Anti-CSRF Cookie
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        if (!string.IsNullOrEmpty(tokens.RequestToken))
        {
            Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken, new CookieOptions
            {
                HttpOnly = false,
                Secure = Request.IsHttps,
                SameSite = Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax,
                Path = "/"
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = result.Message ?? "Login successful.",
            Data = new
            {
                result.UserId,
                result.UserName,
                result.Email,
                result.Roles,
                result.CompanyId,
                result.CompanyName,
                result.RequiresCompanySelection,
                Theme = command.Theme,
                Language = command.Language,
                RememberMe = command.RememberMe
            }
        });
    }

    /// <summary>
    /// Refresh access token using HttpOnly refresh token cookie (or optional body payload).
    /// </summary>
    [HttpPost("refresh-token")]
    [EndpointSummary("Create refresh token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DefaultResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DefaultResponseModel), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto? request = null)
    {
        string? accessToken = request?.AccessToken;
        string? refreshToken = request?.RefreshToken;

        if (string.IsNullOrEmpty(refreshToken))
        {
            refreshToken = _cookieService.GetRefreshToken();
        }

        if (string.IsNullOrEmpty(accessToken))
        {
            accessToken = _cookieService.GetAccessToken();
        }

        if (string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Success = false,
                Message = "Missing refresh token.",
                Data = null
            });
        }

        AuthResultDto result = await Mediator.Send(new RefreshTokenCommand
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        });

        if (!result.Succeeded)
        {
            _cookieService.ClearAuthCookies();
            return BadRequest(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Success = false,
                Message = result.Message ?? "Invalid refresh token request.",
                Data = result
            });
        }

        DateTime accessExpiry = result.Expiry ?? DateTime.UtcNow.AddMinutes(15);
        DateTime refreshExpiry = DateTime.UtcNow.AddDays(15);
        _cookieService.SetAuthCookies(result.AccessToken ?? string.Empty, result.RefreshToken ?? string.Empty, accessExpiry, refreshExpiry, true);

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = result.Message ?? "Token refreshed successfully.",
            Data = new
            {
                result.UserId,
                result.UserName,
                result.Email,
                result.Roles,
                result.CompanyId,
                result.CompanyName,
                result.RequiresCompanySelection
            }
        });
    }

    /// <summary>
    /// Logout current user and clear all auth cookies.
    /// </summary>
    [HttpPost("logout")]
    [EndpointSummary("Logout account")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DefaultResponseModel), StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        _cookieService.ClearAuthCookies();
        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Logged out successfully.",
            Data = null
        });
    }

    [HttpGet("companies")]
    [Authorize(Roles = "SuperAdmin,Administrator")]
    [EndpointSummary("Get company accounts available to platform administrators")]
    public async Task<IActionResult> GetCompanyAccounts(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetCompanyAccountsQuery(), cancellationToken);
        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Data = result
        });
    }

    [HttpPost("select-company")]
    [Authorize(Roles = "SuperAdmin,Administrator")]
    [EndpointSummary("Select the current company account for this administrator session")]
    public async Task<IActionResult> SelectCompany([FromBody] SelectCompanyCommand command)
    {
        var result = await Mediator.Send(command);
        if (!result.Succeeded)
            return BadRequest(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Success = false,
                Message = result.Message
            });

        _cookieService.SetAuthCookies(result.AccessToken!, result.RefreshToken!,
            result.Expiry ?? DateTime.UtcNow.AddMinutes(15), DateTime.UtcNow.AddDays(15), command.RememberMe);
        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Company account selected successfully.",
            Data = new CurrentUserDto
            {
                UserId = result.UserId,
                UserName = result.UserName,
                Email = result.Email,
                Roles = result.Roles,
                CompanyId = result.CompanyId,
                CompanyName = result.CompanyName,
                RequiresCompanySelection = result.RequiresCompanySelection
            }
        });
    }

    /// <summary>
    /// Get current authenticated user profile and preferences.
    /// </summary>
    [HttpGet("me")]
    [EndpointSummary("Get current user profile")]
    [Authorize]
    [ProducesResponseType(typeof(DefaultResponseModel), StatusCodes.Status200OK)]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;
        var userName = User.Identity?.Name 
                       ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "User profile retrieved successfully.",
            Data = new CurrentUserDto
            {
                UserId = userId,
                UserName = userName,
                Email = email,
                Roles = roles,
                CompanyId = int.TryParse(User.FindFirst("company_id")?.Value, out var companyId) ? companyId : null,
                CompanyName = User.FindFirst("company_name")?.Value,
                RequiresCompanySelection = (User.IsInRole("SuperAdmin") || User.IsInRole("Administrator"))
                    && !User.HasClaim(claim => claim.Type == "company_id")
            }
        });
    }

    /// <summary>
    /// Generate and set Anti-CSRF token cookie.
    /// </summary>
    [HttpGet("csrf-token")]
    [EndpointSummary("Get CSRF token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DefaultResponseModel), StatusCodes.Status200OK)]
    public IActionResult GetCsrfToken()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        if (!string.IsNullOrEmpty(tokens.RequestToken))
        {
            Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken, new CookieOptions
            {
                HttpOnly = false,
                Secure = Request.IsHttps,
                SameSite = Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax,
                Path = "/"
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "CSRF token generated successfully.",
            Data = new { CsrfToken = tokens.RequestToken }
        });
    }

    /// <summary>
    /// Generate a password reset token for a user by email.
    /// </summary>
    [HttpPost("generate-reset-token")]
    [EndpointSummary("create reset-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DefaultResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DefaultResponseModel), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateResetToken([FromBody] GenerateResetTokenCommand command)
    {
        var token = await Mediator.Send(command);
        if (token == null)
        {
            return BadRequest(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Success = false,
                Message = "User not found or unable to generate reset token.",
                Data = null
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Password reset token generated successfully.",
            Data = new { Token = token }
        });
    }

    /// <summary>
    /// Reset password using reset token.
    /// </summary>
    [HttpPost("reset-password")]
    [EndpointSummary("Reset Password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DefaultResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DefaultResponseModel), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        var result = await Mediator.Send(command);
        if (!result.Succeeded)
        {
            return BadRequest(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Success = false,
                Message = result.Message ?? "Password reset failed.",
                Data = result.Errors
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = result.Message ?? "Password reset successfully.",
            Data = null
        });
    }

    /// <summary>
    /// Change password for the currently authenticated user.
    /// </summary>
    [HttpPost("change-password")]
    [EndpointSummary("Change Password")]
    [Authorize]
    [ProducesResponseType(typeof(DefaultResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DefaultResponseModel), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command)
    {
        var result = await Mediator.Send(command);
        if (!result.Succeeded)
        {
            return BadRequest(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Success = false,
                Message = result.Message ?? "Password change failed.",
                Data = result.Errors
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = result.Message ?? "Password changed successfully.",
            Data = null
        });
    }
}

public class RefreshTokenRequestDto
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
}
