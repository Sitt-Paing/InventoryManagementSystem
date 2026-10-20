using InventoryManagementSystem.Application.Auth.Models;
using InventoryManagementSystem.Application.Common.Interfaces;
using MediatR;

namespace InventoryManagementSystem.Application.Auth.Commands.SelectCompany;

public class SelectCompanyCommandHandler : IRequestHandler<SelectCompanyCommand, AuthResultDto>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;

    public SelectCompanyCommandHandler(IIdentityService identityService, ICurrentUserService currentUserService)
    {
        _identityService = identityService;
        _currentUserService = currentUserService;
    }

    public Task<AuthResultDto> Handle(SelectCompanyCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentUserService.UserId))
            throw new UnauthorizedAccessException("Login is required.");
        return _identityService.SelectCompanyAsync(_currentUserService.UserId, request.CompanyId);
    }
}
