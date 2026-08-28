using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Products.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Products.Queries.GetProductsById;

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
            Brand = product.Brand,
            Unit = product.Unit,
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
