using InventoryManagementSystem.Application.Products.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Products.Queries.GetProductsById;

public record GetProductByIdQuery(string ProductId) : IRequest<ProductDto>;