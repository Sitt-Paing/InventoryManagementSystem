using System;
using System.Collections.Generic;
using System.Linq;

namespace InventoryManagementSystem.Application.Auth.Models;

public class RegisterRequest
{
    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? Role { get; set; }
}

public class LoginRequest
{
    public string UserNameOrEmail { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class RefreshTokenRequest
{
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
}

public class ResetPasswordRequest
{
    public string Email { get; set; } = null!;
    public string Token { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}

public class ResultDto
{
    public bool Succeeded { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ResultDto Success(string? message = null) => new() { Succeeded = true, Message = message };
    public static ResultDto Failure(IEnumerable<string> errors, string? message = null) => new() { Succeeded = false, Message = message, Errors = errors.ToList() };
    public static ResultDto Failure(string error, string? message = null) => new() { Succeeded = false, Message = message, Errors = new List<string> { error } };
}

public class AuthResultDto
{
    public bool Succeeded { get; set; }
    public string? Message { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? Expiry { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
