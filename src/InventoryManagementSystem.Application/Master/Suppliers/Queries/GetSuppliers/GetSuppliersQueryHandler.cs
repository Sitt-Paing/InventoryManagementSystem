using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Master.Suppliers.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Master.Suppliers.Queries.GetSuppliers;

public class GetSuppliersQueryHandler : IRequestHandler<GetSuppliersQuery, List<SupplierDto>>
{
    private readonly IApplicationDbContext context;
    public GetSuppliersQueryHandler(IApplicationDbContext _context)
    {
        this.context = _context;
    }

    public async Task<List<SupplierDto>> Handle(GetSuppliersQuery request, CancellationToken cancellationToken)
    {
        var suppliers = await context.Suppliers.AsNoTracking().Where(x => !x.DeletedOn.HasValue).ToListAsync(cancellationToken);
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
            Status = s.Status,
            CreatedOn = s.CreatedOn,
            CreatedBy = s.CreatedBy,
            UpdatedOn = s.UpdatedOn,
            UpdatedBy = s.UpdatedBy,
        }).ToList();
    }
}
