using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.ProductUomConversions.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Application.ProductUomConversions.Queries.GetProductUomConversionsByProductId;

public class GetProductUomConversionsByProductIdQueryHandler
    : IRequestHandler<GetProductUomConversionsByProductIdQuery, List<ProductUomConversionDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProductUomConversionsByProductIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductUomConversionDto>> Handle(
        GetProductUomConversionsByProductIdQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.ProductUomConversions
            .AsNoTracking()
            .Include(x => x.FromUom)
            .Include(x => x.ToUom)
            .Where(x => x.ProductId == request.ProductId && !x.DeletedOn.HasValue)
            .OrderBy(x => x.ConversionFactor)
            .Select(x => new ProductUomConversionDto
            {
                Id = x.Id,
                ProductId = x.ProductId,
                FromUomId = x.FromUomId,
                FromUomCode = x.FromUom != null ? x.FromUom.Code : null,
                FromUomName = x.FromUom != null ? x.FromUom.Name : null,
                ToUomId = x.ToUomId,
                ToUomCode = x.ToUom != null ? x.ToUom.Code : null,
                ToUomName = x.ToUom != null ? x.ToUom.Name : null,
                ConversionFactor = x.ConversionFactor,
                Barcode = x.Barcode,
                IsDefaultPurchase = x.IsDefaultPurchase,
                IsDefaultSale = x.IsDefaultSale,
                IsActive = x.IsActive,
                CreatedOn = x.CreatedOn,
                CreatedBy = x.CreatedBy,
                UpdatedOn = x.UpdatedOn,
                UpdatedBy = x.UpdatedBy,
                DeletedOn = x.DeletedOn,
                DeletedBy = x.DeletedBy
            })
            .ToListAsync(cancellationToken);
    }
}
