using InventoryManagementSystem.Application.ProductUomConversions.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.ProductUomConversions.Commands.UpdateProductUomConversion;

public record UpdateProductUomConversionCommand(
    long Id,
    long FromUomId,
    long ToUomId,
    decimal ConversionFactor,
    string? Barcode,
    bool IsDefaultPurchase,
    bool IsDefaultSale,
    bool IsActive
) : IRequest<ProductUomConversionDto?>;
