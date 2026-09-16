using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.ProductUomConversions.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Application.ProductUomConversions.Commands.DeleteProductUomConversion;

public class DeleteProductUomConversionCommandHandler
    : IRequestHandler<DeleteProductUomConversionCommand, ProductUomConversionDto?>
{
    private readonly IApplicationDbContext _context;

    public DeleteProductUomConversionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductUomConversionDto?> Handle(
        DeleteProductUomConversionCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _context.ProductUomConversions
            .Include(x => x.FromUom)
            .Include(x => x.ToUom)
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.DeletedOn.HasValue, cancellationToken);

        if (entity == null) return null;

        var dto = new ProductUomConversionDto
        {
            Id = entity.Id,
            ProductId = entity.ProductId,
            FromUomId = entity.FromUomId,
            FromUomCode = entity.FromUom?.Code,
            FromUomName = entity.FromUom?.Name,
            ToUomId = entity.ToUomId,
            ToUomCode = entity.ToUom?.Code,
            ToUomName = entity.ToUom?.Name,
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

        _context.ProductUomConversions.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return dto;
    }
}
