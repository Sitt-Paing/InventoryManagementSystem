using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Products.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Products.Commands.DeleteProduct;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, ProductDto?>
{
    private readonly IApplicationDbContext _context;
    public DeleteProductCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<ProductDto?> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Products
            .Where(x => !x.DeletedOn.HasValue)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (entity == null) return null;
        entity.DeletedOn = DateTime.UtcNow;
        entity.DeletedBy = entity.DeletedBy; // You can replace this with the actual user performing the deletion
        await _context.SaveChangesAsync(cancellationToken);
        return new ProductDto
        {
            Id = entity.Id,
            Name = entity.Name,
            CategoryId = entity.CategoryId,
            Brand = entity.Brand,
            Unit = entity.Unit,
            Sku = entity.Sku,
            Barcode = entity.Barcode,
            CostPrice = entity.CostPrice,
            SellingPrice = entity.SellingPrice,
            CurrentStock = entity.CurrentStock,
            ReorderLevel = entity.ReorderLevel,
            ReorderQuantity = entity.ReorderQuantity,
            Tax = entity.Tax,
            Status = entity.Status,
            Description = entity.Description,
            CreatedOn = entity.CreatedOn,
            CreatedBy = entity.CreatedBy,
            UpdatedOn = entity.UpdatedOn,
            UpdatedBy = entity.UpdatedBy,
            DeletedOn = entity.DeletedOn,
            DeletedBy = entity.DeletedBy
        };
    }
}
