using InventoryManagementSystem.Application.Master.Products.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Master.Products.Queries.GetProducts;

public record GetProductsQuery(long? CategoryId = null) : IRequest<List<ProductDto>>;