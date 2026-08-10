using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Products.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Products.Commands.UpdateProduct;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto?> 
{
    private readonly IApplicationDbContext _context;
    public UpdateProductCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductDto?> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Products
            .AsNoTracking()
            .Where(x => !x.DeletedOn.HasValue)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity == null) return null;

        entity.Name = request.Name;
        entity.CategoryId = request.CategoryId;
        entity.Sku = request.Sku;
        entity.CurrentStock = request.CurrentStock;
        entity.UnitPrice = request.UnitPrice;
        entity.ReorderLevel = request.ReorderLevel;

        await _context.SaveChangesAsync(cancellationToken);

        return new ProductDto
        {
            Id = entity.Id,
            Name = entity.Name,
            CategoryId = entity.CategoryId,
            Sku = entity.Sku,
            CurrentStock = entity.CurrentStock,
            ReorderLevel = entity.ReorderLevel,
            UnitPrice = entity.UnitPrice,
            CreatedOn = entity.CreatedOn,
            CreatedBy = entity.CreatedBy,
            UpdatedOn = entity.UpdatedOn,
            UpdatedBy = entity.UpdatedBy,
            DeletedOn = entity.DeletedOn,
            DeletedBy = entity.DeletedBy
        };
    }
}

