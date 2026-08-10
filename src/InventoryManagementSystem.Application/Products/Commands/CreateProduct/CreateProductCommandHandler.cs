using System;
using System.Linq;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Products.DTOs;
using InventoryManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Products.Commands.CreateProduct;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IApplicationDbContext _context;

    public CreateProductCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        string sku = request.Sku;
        if (string.IsNullOrWhiteSpace(sku))
        {
            sku = await GenerateSkuAsync(request.Name, cancellationToken);
        }

        var entity = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            CategoryId = request.categoryId,
            UnitPrice = request.UnitPrice,
            Sku = sku,
            Barcode = request.Barcode,
            CurrentStock = request.CurrentStock,
            ReorderLevel = request.ReorderLevel
        };

        _context.Products.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return new ProductDto
        {
            Id = entity.Id,
            Name = entity.Name,
            CategoryId = entity.CategoryId,
            UnitPrice = entity.UnitPrice,
            Sku = entity.Sku,
            Barcode = entity.Barcode,
            CurrentStock = entity.CurrentStock,
            ReorderLevel = entity.ReorderLevel,
            CreatedOn = entity.CreatedOn,
            CreatedBy = entity.CreatedBy,
        };
    }

    private async Task<string> GenerateSkuAsync(string productName, CancellationToken cancellationToken)
    {
        var trimmedName = productName.Trim();
        var firstWord = trimmedName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "PRD";

        var cleanPrefix = new string(firstWord.Where(char.IsLetterOrDigit).ToArray());
        if (string.IsNullOrWhiteSpace(cleanPrefix))
        {
            cleanPrefix = "PRD";
        }

        var prefixWithDash = $"{cleanPrefix}-";

        var existingSkus = await _context.Products
            .AsNoTracking()
            .Where(p => p.Sku != null && p.Sku.StartsWith(prefixWithDash))
            .Select(p => p.Sku!)
            .ToListAsync(cancellationToken);

        int maxNumber = 0;
        foreach (var existingSku in existingSkus)
        {
            var numberPart = existingSku.Substring(prefixWithDash.Length);
            if (int.TryParse(numberPart, out int num))
            {
                if (num > maxNumber) maxNumber = num;
            }
        }

        int nextNumber = maxNumber + 1;
        return $"{cleanPrefix}-{nextNumber:D3}";
    }
}
