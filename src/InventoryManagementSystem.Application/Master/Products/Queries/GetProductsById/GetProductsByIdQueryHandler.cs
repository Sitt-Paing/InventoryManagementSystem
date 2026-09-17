using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Master.Products.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Master.Products.Queries.GetProductsById;

public class GetProductsByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    private readonly IApplicationDbContext _context;
    public GetProductsByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Where(p => !p.DeletedOn.HasValue)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);
        if (product == null) return null;
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            CategoryId = product.CategoryId,
            BaseUomId = product.BaseUomId,
            BaseUomCode = product.BaseUom != null ? product.BaseUom.Code : null,
            BaseUomName = product.BaseUom != null ? product.BaseUom.Name : null,
            PurchaseUomId = product.PurchaseUomId,
            PurchaseUomCode = product.PurchaseUom != null ? product.PurchaseUom.Code : null,
            PurchaseUomName = product.PurchaseUom != null ? product.PurchaseUom.Name : null,
            SaleUomId = product.SaleUomId,
            SaleUomCode = product.SaleUom != null ? product.SaleUom.Code : null,
            SaleUomName = product.SaleUom != null ? product.SaleUom.Name : null,
            Brand = product.Brand,
            Unit = product.Unit ?? (product.BaseUom != null ? product.BaseUom.Code : null),
            Sku = product.Sku,
            Barcode = product.Barcode,
            CostPrice = product.CostPrice,
            SellingPrice = product.SellingPrice,
            CurrentStock = product.CurrentStock,
            ReorderLevel = product.ReorderLevel,
            ReorderQuantity = product.ReorderQuantity,
            Tax = product.Tax,
            Status = product.Status,
            Description = product.Description,
            CreatedOn = product.CreatedOn,
            CreatedBy = product.CreatedBy,
            UpdatedOn = product.UpdatedOn,
            UpdatedBy = product.UpdatedBy,
            DeletedOn = product.DeletedOn,
            DeletedBy = product.DeletedBy
        };
    }
}
