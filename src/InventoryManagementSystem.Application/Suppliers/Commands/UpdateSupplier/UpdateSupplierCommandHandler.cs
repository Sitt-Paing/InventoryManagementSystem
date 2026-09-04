using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Suppliers.DTOs;
using InventoryManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace InventoryManagementSystem.Application.Suppliers.Commands.UpdateSupplier;

public class UpdateSupplierCommandHandler : IRequestHandler<UpdateSupplierCommand, SupplierDto?>
{
    private readonly IApplicationDbContext _context;

    public UpdateSupplierCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SupplierDto?> Handle(UpdateSupplierCommand command, CancellationToken cancellationToken)
    {
        Supplier? supplier = await _context.Suppliers
            .Where(x => !x.DeletedOn.HasValue)
            .FirstOrDefaultAsync(x => x.Id == command.id, cancellationToken);

        if (supplier == null) return null;

        supplier.SupplierCode = command.supplierCode;
        supplier.CompanyName = command.companyName;
        supplier.ContactPerson = command.contactPerson;
        supplier.Email = command.email;
        supplier.Phone = command.phone;
        supplier.Address = command.address;
        supplier.PaymentTerms = command.paymentTerms;
        supplier.CreditLimit = command.creditLimit;
        supplier.Status = command.status;
        await _context.SaveChangesAsync(cancellationToken);
        return new SupplierDto
        {
            Id = supplier.Id,
            SupplierCode = supplier.SupplierCode,
            CompanyName = supplier.CompanyName,
            ContactPerson = supplier.ContactPerson,
            Email = supplier.Email,
            Phone = supplier.Phone,
            Address = supplier.Address,
            PaymentTerms = supplier.PaymentTerms,
            CreditLimit = supplier.CreditLimit,
            Status = supplier.Status,
            CreatedOn = supplier.CreatedOn,
            CreatedBy = supplier.CreatedBy,
            UpdatedOn = supplier.UpdatedOn,
            UpdatedBy = supplier.UpdatedBy,
            DeletedOn = supplier.DeletedOn,
            DeletedBy = supplier.DeletedBy
        };
    }
}

