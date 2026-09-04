using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Suppliers.DTOs;
using InventoryManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Suppliers.Commands.DeleteSupplier;

public class DeleteSupplierCommandHandler: IRequestHandler<DeleteSupplierCommand, SupplierDto?> 
{ 
    private readonly IApplicationDbContext _context;

    public DeleteSupplierCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SupplierDto?> Handle(DeleteSupplierCommand command, CancellationToken cancellationToken)
    {
        Supplier? supplier = await _context.Suppliers.Where(x => !x.DeletedOn.HasValue).FirstOrDefaultAsync(x => x.Id == command.id, cancellationToken);
        if (supplier == null) return null;
        
        supplier.DeletedOn = DateTime.UtcNow;
        supplier.DeletedBy = supplier.DeletedBy;
        _context.Suppliers.Update(supplier);
        await _context.SaveChangesAsync(cancellationToken);
        return new SupplierDto
        {
            Id = supplier.Id,
            SupplierCode = supplier.SupplierCode,
            CompanyName = supplier.CompanyName,
            ContactPerson = supplier.ContactPerson,
            Phone = supplier.Phone,
            Email = supplier.Email,
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
