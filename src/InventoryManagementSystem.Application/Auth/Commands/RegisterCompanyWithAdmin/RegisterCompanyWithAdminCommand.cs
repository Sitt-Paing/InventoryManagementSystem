using FluentValidation;
using InventoryManagementSystem.Application.Auth.Models;
using MediatR;

namespace InventoryManagementSystem.Application.Auth.Commands.RegisterCompanyWithAdmin;

public record RegisterCompanyWithAdminCommand : IRequest<AuthResultDto>
{
    public string CompanyName { get; init; } = string.Empty;
    public string ContactPerson { get; init; } = string.Empty;
    public string CompanyEmail { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;

    public string AdminUserName { get; init; } = string.Empty;
    public string AdminEmail { get; init; } = string.Empty;
    public string AdminPassword { get; init; } = string.Empty;
    public string Role { get; init; } = "CompanyAdmin";
}

public class RegisterCompanyWithAdminValidator : AbstractValidator<RegisterCompanyWithAdminCommand>
{
    public RegisterCompanyWithAdminValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().WithMessage("Company Name is required.");
        RuleFor(x => x.AdminUserName).NotEmpty().WithMessage("Admin Username is required.");
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress().WithMessage("Valid Admin Email is required.");
        RuleFor(x => x.AdminPassword).NotEmpty().MinimumLength(6).WithMessage("Password must be at least 6 characters.");
    }
}