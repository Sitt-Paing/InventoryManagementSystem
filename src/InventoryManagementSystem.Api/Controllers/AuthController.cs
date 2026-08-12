using InventoryManagementSystem.Application.Auth.Commands.ChangePassword;
using InventoryManagementSystem.Application.Auth.Commands.GenerateResetToken;
using InventoryManagementSystem.Application.Auth.Commands.LoginUser;
using InventoryManagementSystem.Application.Auth.Commands.RefreshToken;
using InventoryManagementSystem.Application.Auth.Commands.RegisterUser;
using InventoryManagementSystem.Application.Auth.Commands.ResetPassword;
using InventoryManagementSystem.Application.Auth.Models;
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
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command)
    {
        var result = await Mediator.Send(command);
        if (!result.Succeeded)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Authenticate a user by Username or Email and issue JWT tokens.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand command)
    {
        var result = await Mediator.Send(command);
        if (!result.Succeeded)
        {
            return Unauthorized(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Refresh access token using a valid refresh token.
    /// </summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command)
    {
        var result = await Mediator.Send(command);
        if (!result.Succeeded)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Generate a password reset token for a user by email.
    /// </summary>
    [HttpPost("generate-reset-token")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateResetToken([FromBody] GenerateResetTokenCommand command)
    {
        var token = await Mediator.Send(command);
        if (token == null)
        {
            return BadRequest(new { message = "User not found or unable to generate reset token." });
        }
        return Ok(new { token });
    }

    /// <summary>
    /// Reset password using reset token.
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        var result = await Mediator.Send(command);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }
        return Ok(new { message = "Password reset successfully." });
    }

    /// <summary>
    /// Change password for the currently authenticated user.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command)
    {
        var result = await Mediator.Send(command);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }
        return Ok(new { message = "Password changed successfully." });
    }
}
