using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Suppliers.DTOs;
using InventoryManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace InventoryManagementSystem.Application.Suppliers.Commands.CreateSupplier;

public class CreateSupplierCommandHandler : IRequestHandler<CreateSupplierCommand, SupplierDto>
{
    private readonly IApplicationDbContext context;

    public CreateSupplierCommandHandler(IApplicationDbContext _context)
    {
        context = _context;
    }

    public async Task<SupplierDto> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
    {
        string code = request.supplierCode;
        if (string.IsNullOrWhiteSpace(request.supplierCode))
        {
            code = await GenerateSupplierCodeAsync(request.companyName, cancellationToken);
        }

        bool existingCode = await context.Suppliers.AnyAsync(x => !x.DeletedOn.HasValue && x.SupplierCode == request.supplierCode, cancellationToken);

        if(existingCode)
        {
            throw new InvalidOperationException($"Supplier Code is already existed");
        }
  
        Supplier entity = new Supplier
        {
            SupplierCode = code,
            CompanyName = request.companyName,
            ContactPerson = request.contactPerson,
            Phone = request.phone,
            Email = request.email,
            Address = request.address,
            PaymentTerms = request.paymentTerms,
            CreditLimit = request.creditLimit,
            Status = request.status
        };
        context.Suppliers.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return new SupplierDto
        {
            Id = entity.Id,
            SupplierCode = entity.SupplierCode,
            CompanyName = entity.CompanyName,
            ContactPerson = entity.ContactPerson,
            Phone = entity.Phone,
            Email = entity.Email,
            Address = entity.Address,
            PaymentTerms = entity.PaymentTerms,
            CreditLimit = entity.CreditLimit,
            Status = entity.Status,
            CreatedOn = entity.CreatedOn,
            CreatedBy = entity.CreatedBy
        };
    }

    private async Task<string> GenerateSupplierCodeAsync(string SupplierName, CancellationToken cancellationToken)
    {
        string trimmedName = SupplierName.Trim();
        string firstWord = trimmedName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "SUP";
        string cleanPrefix = new string(firstWord.Where(char.IsLetterOrDigit).ToArray());

        if (string.IsNullOrWhiteSpace(cleanPrefix))
        {
            cleanPrefix = "SUP";
        }

        string prefixWithDash = $"{cleanPrefix}-";
        var existingSupplierCodes = await context.Suppliers
            .AsNoTracking()
            .Where(x => x.SupplierCode != null && x.SupplierCode.StartsWith(prefixWithDash))
            .Select(x => x.SupplierCode)
            .ToListAsync(cancellationToken);

        int maxNumber = 0;
        foreach (var existingSupplierCode in existingSupplierCodes)
        {
            var numberPart = existingSupplierCode.Substring(prefixWithDash.Length);
            if (int.TryParse(numberPart, out int num))
            {
                if (num > maxNumber) maxNumber = num;
            }
        }

        int nextNumber = maxNumber + 1;
        return $"{cleanPrefix}-{nextNumber:D3}";
    }
}