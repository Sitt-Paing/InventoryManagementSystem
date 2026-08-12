using FluentValidation;
using InventoryManagementSystem.Application.Auth.Models;
using InventoryManagementSystem.Application.Common.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Application.Auth.Commands.RefreshToken;

public record RefreshTokenCommand : IRequest<AuthResultDto>
{
    public string AccessToken { get; init; } = null!;
    public string RefreshToken { get; init; } = null!;
}

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(v => v.AccessToken)
            .NotEmpty().WithMessage("AccessToken is required.");

        RuleFor(v => v.RefreshToken)
            .NotEmpty().WithMessage("RefreshToken is required.");
    }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResultDto>
{
    private readonly IIdentityService _identityService;

    public RefreshTokenCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<AuthResultDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var refreshRequest = new RefreshTokenRequest
        {
            AccessToken = request.AccessToken,
            RefreshToken = request.RefreshToken
        };

        return await _identityService.RefreshTokenAsync(refreshRequest);
    }
}
