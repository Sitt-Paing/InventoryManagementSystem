using InventoryManagementSystem.Application.Master.ProductUomConversions.DTOs;
using MediatR;
using System;
using System.Collections.Generic;

namespace InventoryManagementSystem.Application.Master.ProductUomConversions.Queries.GetProductUomConversionsByProductId;

public record GetProductUomConversionsByProductIdQuery(Guid ProductId) : IRequest<List<ProductUomConversionDto>>;
