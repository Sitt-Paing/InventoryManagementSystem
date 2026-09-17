using InventoryManagementSystem.Application.Master.ProductUomConversions.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Master.ProductUomConversions.Commands.DeleteProductUomConversion;

public record DeleteProductUomConversionCommand(long Id) : IRequest<ProductUomConversionDto?>;
