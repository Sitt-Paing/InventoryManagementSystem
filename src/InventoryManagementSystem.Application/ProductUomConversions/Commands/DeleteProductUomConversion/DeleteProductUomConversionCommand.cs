using InventoryManagementSystem.Application.ProductUomConversions.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.ProductUomConversions.Commands.DeleteProductUomConversion;

public record DeleteProductUomConversionCommand(long Id) : IRequest<ProductUomConversionDto?>;
