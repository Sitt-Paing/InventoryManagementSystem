using System;
using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Process.PurchaseOrders.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Process.PurchaseOrders.Queries.GetPurchaseOrders;

public record GetPurchaseOrdersQuery(
    string? Q = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    int? SupplierId = null,
    int? WarehouseId = null,
    bool? Status = null,
    string? SortField = null,
    int Order = -1,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PagedResult<PurchaseOrderDto>>;
