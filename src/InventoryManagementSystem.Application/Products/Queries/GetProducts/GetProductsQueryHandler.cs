using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Products.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Products.Queries.GetProducts;


public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, List<ProductDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProductsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<List<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => !p.DeletedOn.HasValue)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                CurrentStock = p.CurrentStock,
                ReorderLevel = p.ReorderLevel,
                Sku = p.Sku,
                Barcode = p.Barcode,
                UnitPrice = p.UnitPrice,
                CategoryId = p.CategoryId,
                CreatedOn = p.CreatedOn,
                CreatedBy = p.CreatedBy,
                UpdatedOn = p.UpdatedOn,
                UpdatedBy = p.UpdatedBy,
                DeletedOn = p.DeletedOn,
                DeletedBy = p.DeletedBy
            })
            .ToListAsync(cancellationToken);
    }
}