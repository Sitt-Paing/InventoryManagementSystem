using FluentValidation;
using InventoryManagementSystem.Application.Common.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Application.Auth.Commands.GenerateResetToken;

public record GenerateResetTokenCommand : IRequest<string?>
{
    public string Email { get; init; } = null!;
}

public class GenerateResetTokenCommandValidator : AbstractValidator<GenerateResetTokenCommand>
{
    public GenerateResetTokenCommandValidator()
    {
        RuleFor(v => v.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.");
    }
}

public class GenerateResetTokenCommandHandler : IRequestHandler<GenerateResetTokenCommand, string?>
{
    private readonly IIdentityService _identityService;

    public GenerateResetTokenCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<string?> Handle(GenerateResetTokenCommand request, CancellationToken cancellationToken)
    {
        return await _identityService.GeneratePasswordResetTokenAsync(request.Email);
    }
}
