using InventoryManagementSystem.Application.Master.ProductUomConversions.DTOs;
using MediatR;
using System;

namespace InventoryManagementSystem.Application.Master.ProductUomConversions.Commands.CreateProductUomConversion;

public record CreateProductUomConversionCommand(
    Guid ProductId,
    long FromUomId,
    long ToUomId,
    decimal ConversionFactor,
    string? Barcode,
    bool IsDefaultPurchase,
    bool IsDefaultSale
) : IRequest<ProductUomConversionDto>;
