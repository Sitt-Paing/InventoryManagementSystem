using InventoryManagementSystem.Application.Master.Products.DTOs;
using MediatR;
using InventoryManagementSystem.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Master.Products.Queries.GetProducts;

public record GetProductsQuery(long? CategoryId = null, string? Search = null, int? PageNumber = null, int? PageSize = null) : IRequest<PagedResult<ProductDto>>;