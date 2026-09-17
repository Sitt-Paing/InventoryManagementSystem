using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Master.Products.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Master.Products.Queries.GetProducts;


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
                BaseUomId = p.BaseUomId,
                BaseUomCode = p.BaseUom != null ? p.BaseUom.Code : null,
                BaseUomName = p.BaseUom != null ? p.BaseUom.Name : null,
                PurchaseUomId = p.PurchaseUomId,
                PurchaseUomCode = p.PurchaseUom != null ? p.PurchaseUom.Code : null,
                PurchaseUomName = p.PurchaseUom != null ? p.PurchaseUom.Name : null,
                SaleUomId = p.SaleUomId,
                SaleUomCode = p.SaleUom != null ? p.SaleUom.Code : null,
                SaleUomName = p.SaleUom != null ? p.SaleUom.Name : null,
                Brand = p.Brand,
                Unit = p.Unit ?? (p.BaseUom != null ? p.BaseUom.Code : null),
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