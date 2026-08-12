using FluentValidation;
using InventoryManagementSystem.Application.Auth.Models;
using InventoryManagementSystem.Application.Common.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Application.Auth.Commands.ChangePassword;

public record ChangePasswordCommand : IRequest<ResultDto>
{
    public string CurrentPassword { get; init; } = null!;
    public string NewPassword { get; init; } = null!;
}

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(v => v.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");

        RuleFor(v => v.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(6).WithMessage("New password must be at least 6 characters long.");
    }
}

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, ResultDto>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;

    public ChangePasswordCommandHandler(IIdentityService identityService, ICurrentUserService currentUserService)
    {
        _identityService = identityService;
        _currentUserService = currentUserService;
    }

    public async Task<ResultDto> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return ResultDto.Failure("User is not authenticated.");
        }

        var changeRequest = new ChangePasswordRequest
        {
            CurrentPassword = request.CurrentPassword,
            NewPassword = request.NewPassword
        };

        return await _identityService.ChangePasswordAsync(userId, changeRequest);
    }
}
