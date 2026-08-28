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
            .Where(x => !x.DeletedOn.HasValue)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity == null) return null;

        entity.Name = request.Name;
        entity.CategoryId = request.CategoryId;
        entity.Sku = request.Sku;
        entity.Brand = request.Brand;
        entity.Unit = request.Unit;
        entity.Barcode = request.Barcode;
        entity.CostPrice = request.CostPrice;
        entity.SellingPrice = request.SellingPrice;
        entity.CurrentStock = request.CurrentStock;
        entity.ReorderLevel = request.ReorderLevel;
        entity.ReorderQuantity = request.ReorderQuantity;
        entity.Tax = request.Tax;
        entity.Status = request.Status;
        entity.Description = request.Description;

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

