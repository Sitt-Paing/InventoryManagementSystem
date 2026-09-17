using InventoryManagementSystem.Application.Master.Products.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Master.Products.Queries.GetProductsById;

public record GetProductByIdQuery(Guid ProductId) : IRequest<ProductDto>;