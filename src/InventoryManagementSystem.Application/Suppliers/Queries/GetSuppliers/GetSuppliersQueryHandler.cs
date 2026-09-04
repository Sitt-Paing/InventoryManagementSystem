using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Suppliers.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Suppliers.Queries.GetSuppliers;

public class GetSuppliersQueryHandler : IRequestHandler<GetSuppliersQuery, List<SupplierDto>>
{
    private readonly IApplicationDbContext context;
    public GetSuppliersQueryHandler(IApplicationDbContext _context)
    {
        this.context = _context;
    }

    public async Task<List<SupplierDto>> Handle(GetSuppliersQuery request, CancellationToken cancellationToken)
    {
        var suppliers = await context.Suppliers.AsNoTracking().ToListAsync(cancellationToken);
        return suppliers.Select(s => new SupplierDto
        {
            Id = s.Id,
            SupplierCode = s.SupplierCode,
            CompanyName = s.CompanyName,
            ContactPerson = s.ContactPerson,
            Phone = s.Phone,
            Email = s.Email,
            Address = s.Address,
            PaymentTerms = s.PaymentTerms,
            CreditLimit = s.CreditLimit,
            Status = s.Status
        }).ToList();
    }
}
