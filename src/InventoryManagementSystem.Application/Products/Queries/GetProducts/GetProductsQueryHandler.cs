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
        var query = _context.Products
            .AsNoTracking()
            .Where(p => !p.DeletedOn.HasValue);

        if (request.CategoryId.HasValue && request.CategoryId.Value > 0)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        }

        return await query
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                CategoryId = p.CategoryId,
                Brand = p.Brand,
                Unit = p.Unit,
                Sku = p.Sku,
                Barcode = p.Barcode,
                CostPrice = p.CostPrice,
                SellingPrice = p.SellingPrice,
                CurrentStock = p.CurrentStock,
                ReorderLevel = p.ReorderLevel,
                ReorderQuantity = p.ReorderQuantity,
                Tax = p.Tax,
                Status = p.Status,
                Description = p.Description,
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