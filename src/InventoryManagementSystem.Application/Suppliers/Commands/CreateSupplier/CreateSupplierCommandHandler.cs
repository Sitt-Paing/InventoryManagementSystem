using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Suppliers.DTOs;
using InventoryManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Suppliers.Commands.CreateSupplier;

public class CreateSupplierCommandHandler : IRequestHandler<CreateSupplierCommand, SupplierDto>
{
    private readonly IApplicationDbContext context;

    public CreateSupplierCommandHandler(IApplicationDbContext _context)
    {
        this.context = _context;
    }

    public async Task<SupplierDto> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
    {
        string code = request.supplierCode;
        if (string.IsNullOrWhiteSpace(request.supplierCode))
        {
            code = await GenerateSupplierCodeAsync(request.companyName, cancellationToken);
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
            Status = entity.Status
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