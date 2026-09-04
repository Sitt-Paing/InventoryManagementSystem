using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Suppliers.DTOs;
using InventoryManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Suppliers.Queries.GetSupplierById;

public class GetSupplierByIdHandler : IRequestHandler<GetSupplierByIdQuery, SupplierDto?>
{
    private readonly IApplicationDbContext _context;
    public GetSupplierByIdHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SupplierDto?> Handle(GetSupplierByIdQuery request, CancellationToken cancellationToken)
    {
        var supplier = await _context.Suppliers.Where(x => !x.DeletedOn.HasValue).FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (supplier == null) return null;

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
            Status = supplier.Status
        };
    }
}
