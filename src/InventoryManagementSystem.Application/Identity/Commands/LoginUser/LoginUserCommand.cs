using FluentValidation;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Identity.Models;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Application.Identity.Commands.LoginUser;

public record LoginUserCommand : IRequest<AuthResultDto>
{
    public string UserNameOrEmail { get; init; } = null!;
    public string Password { get; init; } = null!;
}

public class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserCommandValidator()
    {
        RuleFor(v => v.UserNameOrEmail)
            .NotEmpty().WithMessage("Username or Email is required.");

        RuleFor(v => v.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}

public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, AuthResultDto>
{
    private readonly IIdentityService _identityService;

    public LoginUserCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<AuthResultDto> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var loginRequest = new LoginRequest
        {
            UserNameOrEmail = request.UserNameOrEmail,
            Password = request.Password
        };

        return await _identityService.LoginAsync(loginRequest);
    }
}
