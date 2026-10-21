using InventoryManagementSystem.Application.Auth.Models;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.UserManage.SupplierAccounts.Commands.CreateSupplierAccount;

public class CreateSupplierAccountCommandHandler : IRequestHandler<CreateSupplierAccountCommand, ResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityService _identityService;
    public CreateSupplierAccountCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IIdentityService identityService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _identityService = identityService;
    }

    public async Task<ResultDto> Handle(CreateSupplierAccountCommand command, CancellationToken cancellationToken)
    {
        int? companyId = _currentUserService.CompanyId;

        if (!companyId.HasValue)
        {
            return ResultDto.Failure("Select a company first.");
        }

        Supplier? supplier = await _context.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == command.SupplierId && x.Status && !x.DeletedOn.HasValue,
                cancellationToken);

        if (supplier == null)
        {
            return ResultDto.Failure("Active supplier not found.");
        }

        return await _identityService.CreateSupplierAccountAsync(
            command.UserName,
            command.Email,
            command.Password,
            companyId.Value,
            supplier.Id);
    }
}
