using System;
using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Process.GoodReceipts.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Process.GoodReceipts.Queries.GetGoodReceipts;

public record GetGoodReceiptsQuery(
    string? Q = null,
    DateTime? ReceiptDate = null,
    int? SupplierId = null,
    int? WarehouseId = null,
    Guid? PurchaseOrderId = null,
    bool? Status = null,
    string? SortField = null,
    int Order = -1,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PagedResult<GoodReceiptDto>>;
