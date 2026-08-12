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

namespace InventoryManagementSystem.Api.Controllers;

[Route("api/[controller]")]
public class AuthController : ApiControllerBase
{
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
    /// Authenticate a user by Username or Email and issue JWT tokens.
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
                Message = result.Message ?? "Invalid credentials.",
                Data = result
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = result.Message ?? "Login successful.",
            Data = result
        });
    }

    /// <summary>
    /// Refresh access token using a valid refresh token.
    /// </summary>
    [HttpPost("refresh-token")]
    [EndpointSummary("Create refresh token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DefaultResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DefaultResponseModel), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command)
    {
        var result = await Mediator.Send(command);
        if (!result.Succeeded)
        {
            return BadRequest(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Success = false,
                Message = result.Message ?? "Invalid refresh token request.",
                Data = result
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = result.Message ?? "Token refreshed successfully.",
            Data = result
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
