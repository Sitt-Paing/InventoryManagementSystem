using System;
using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Process.PurchaseOrders.DTOs;
using InventoryManagementSystem.Domain.Enums;
using MediatR;

namespace InventoryManagementSystem.Application.Process.PurchaseOrders.Queries.GetPurchaseOrders;

public record GetPurchaseOrdersQuery(
    string? Q = null,
    DateTime? OrderDate = null,
    int? SupplierId = null,
    int? WarehouseId = null,
    PurchaseOrderStatus? Status = null,
    string? SortField = null,
    int Order = -1,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PagedResult<PurchaseOrderDto>>;
