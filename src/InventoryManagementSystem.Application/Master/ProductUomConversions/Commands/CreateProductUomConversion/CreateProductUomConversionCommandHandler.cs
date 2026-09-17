using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Master.ProductUomConversions.DTOs;
using InventoryManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Application.Master.ProductUomConversions.Commands.CreateProductUomConversion;

public class CreateProductUomConversionCommandHandler
    : IRequestHandler<CreateProductUomConversionCommand, ProductUomConversionDto>
{
    private readonly IApplicationDbContext _context;

    public CreateProductUomConversionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductUomConversionDto> Handle(
        CreateProductUomConversionCommand request,
        CancellationToken cancellationToken)
    {
        var entity = new ProductUomConversion
        {
            ProductId = request.ProductId,
            FromUomId = request.FromUomId,
            ToUomId = request.ToUomId,
            ConversionFactor = request.ConversionFactor,
            Barcode = string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode.Trim(),
            IsDefaultPurchase = request.IsDefaultPurchase,
            IsDefaultSale = request.IsDefaultSale,
            IsActive = true
        };

        _context.ProductUomConversions.Add(entity);
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
