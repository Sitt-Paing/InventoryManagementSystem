using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Master.Products.DTOs;
using MediatR;


namespace InventoryManagementSystem.Application.Master.Products.Queries.GetProductsPage;

public record GetProductsPageQuery(long? CategoryId = null, string? Search = null, int PageNumber = 1, int PageSize = 10) : IRequest<PagedResult<ProductListItemDto>>;
