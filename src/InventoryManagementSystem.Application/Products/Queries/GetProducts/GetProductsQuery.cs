using InventoryManagementSystem.Application.Products.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Products.Queries.GetProducts;

public record GetProductsQuery(long? CategoryId = null) : IRequest<List<ProductDto>>;