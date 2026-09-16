using InventoryManagementSystem.Application.ProductUomConversions.DTOs;
using MediatR;
using System;

namespace InventoryManagementSystem.Application.ProductUomConversions.Commands.CreateProductUomConversion;

public record CreateProductUomConversionCommand(
    Guid ProductId,
    long FromUomId,
    long ToUomId,
    decimal ConversionFactor,
    string? Barcode,
    bool IsDefaultPurchase,
    bool IsDefaultSale
) : IRequest<ProductUomConversionDto>;
