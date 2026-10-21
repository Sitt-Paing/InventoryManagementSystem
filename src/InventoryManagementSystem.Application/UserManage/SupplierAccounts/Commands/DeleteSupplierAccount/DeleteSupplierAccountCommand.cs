using InventoryManagementSystem.Application.Auth.Models;
using InventoryManagementSystem.Application.Common.Interfaces;
using MediatR;

namespace InventoryManagementSystem.Application.UserManage.SupplierAccounts.Commands.DeleteSupplierAccount;

public record DeleteSupplierAccountCommand(string UserId) : IRequest<ResultDto>;

public class DeleteSupplierAccountCommandHandler : IRequestHandler<DeleteSupplierAccountCommand, ResultDto>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityService _identityService;

    public DeleteSupplierAccountCommandHandler(ICurrentUserService currentUserService, IIdentityService identityService)
    {
        _currentUserService = currentUserService;
        _identityService = identityService;
    }

    public Task<ResultDto> Handle(DeleteSupplierAccountCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.CompanyId.HasValue)
            return Task.FromResult(ResultDto.Failure("Select a company first."));

        return _identityService.DeleteSupplierAccountAsync(request.UserId, _currentUserService.CompanyId.Value);
    }
}
