using FluentValidation;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Identity.Models;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Application.Identity.Commands.ResetPassword;

public record ResetPasswordCommand : IRequest<ResultDto>
{
    public string Email { get; init; } = null!;
    public string Token { get; init; } = null!;
    public string NewPassword { get; init; } = null!;
}

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(v => v.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.");

        RuleFor(v => v.Token)
            .NotEmpty().WithMessage("Reset token is required.");

        RuleFor(v => v.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(6).WithMessage("New password must be at least 6 characters long.");
    }
}

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ResultDto>
{
    private readonly IIdentityService _identityService;

    public ResetPasswordCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<ResultDto> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var resetRequest = new ResetPasswordRequest
        {
            Email = request.Email,
            Token = request.Token,
            NewPassword = request.NewPassword
        };

        return await _identityService.ResetPasswordAsync(resetRequest);
    }
}
