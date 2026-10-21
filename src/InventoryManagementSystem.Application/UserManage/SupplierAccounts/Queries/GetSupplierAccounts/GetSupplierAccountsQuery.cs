using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.UserManage.SupplierAccounts.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.UserManage.SupplierAccounts.Queries.GetSupplierAccounts;

public record GetSupplierAccountsQuery(int SupplierId) : IRequest<List<SupplierAccountDto>>;

public class GetSupplierAccountsQueryHandler : IRequestHandler<GetSupplierAccountsQuery, List<SupplierAccountDto>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityService _identityService;

    public GetSupplierAccountsQueryHandler(ICurrentUserService currentUserService, IIdentityService identityService)
    {
        _currentUserService = currentUserService;
        _identityService = identityService;
    }

    public Task<List<SupplierAccountDto>> Handle(GetSupplierAccountsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.CompanyId.HasValue)
            throw new InvalidOperationException("Select a company first.");

        return _identityService.GetSupplierAccountsAsync(request.SupplierId, _currentUserService.CompanyId.Value, cancellationToken);
    }
}
