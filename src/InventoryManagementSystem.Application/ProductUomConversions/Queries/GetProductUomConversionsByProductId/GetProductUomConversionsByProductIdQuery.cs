using InventoryManagementSystem.Application.ProductUomConversions.DTOs;
using MediatR;
using System;
using System.Collections.Generic;

namespace InventoryManagementSystem.Application.ProductUomConversions.Queries.GetProductUomConversionsByProductId;

public record GetProductUomConversionsByProductIdQuery(Guid ProductId) : IRequest<List<ProductUomConversionDto>>;
