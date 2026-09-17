using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Master.ProductUomConversions.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Application.Master.ProductUomConversions.Commands.UpdateProductUomConversion;

public class UpdateProductUomConversionCommandHandler
    : IRequestHandler<UpdateProductUomConversionCommand, ProductUomConversionDto?>
{
    private readonly IApplicationDbContext _context;

    public UpdateProductUomConversionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductUomConversionDto?> Handle(
        UpdateProductUomConversionCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _context.ProductUomConversions
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.DeletedOn.HasValue, cancellationToken);

        if (entity == null) return null;

        entity.FromUomId = request.FromUomId;
        entity.ToUomId = request.ToUomId;
        entity.ConversionFactor = request.ConversionFactor;
        entity.Barcode = string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode.Trim();
        entity.IsDefaultPurchase = request.IsDefaultPurchase;
        entity.IsDefaultSale = request.IsDefaultSale;
        entity.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        var fromUom = await _context.UnitOfMeasures.FindAsync(new object[] { entity.FromUomId }, cancellationToken);
        var toUom = await _context.UnitOfMeasures.FindAsync(new object[] { entity.ToUomId }, cancellationToken);

        return new ProductUomConversionDto
        {
            Id = entity.Id,
            ProductId = entity.ProductId,
            FromUomId = entity.FromUomId,
            FromUomCode = fromUom?.Code,
            FromUomName = fromUom?.Name,
            ToUomId = entity.ToUomId,
            ToUomCode = toUom?.Code,
            ToUomName = toUom?.Name,
            ConversionFactor = entity.ConversionFactor,
            Barcode = entity.Barcode,
            IsDefaultPurchase = entity.IsDefaultPurchase,
            IsDefaultSale = entity.IsDefaultSale,
            IsActive = entity.IsActive,
            CreatedOn = entity.CreatedOn,
            CreatedBy = entity.CreatedBy,
            UpdatedOn = entity.UpdatedOn,
            UpdatedBy = entity.UpdatedBy,
            DeletedOn = entity.DeletedOn,
            DeletedBy = entity.DeletedBy
        };
    }
}
