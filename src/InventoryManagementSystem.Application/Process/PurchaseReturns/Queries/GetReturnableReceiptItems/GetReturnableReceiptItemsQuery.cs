using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Process.PurchaseReturns.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Process.PurchaseReturns.Queries.GetReturnableReceiptItems;

public record GetReturnableReceiptItemsQuery(
    int SupplierId,
    string? Q = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    int? PageNumber = null,
    int? PageSize = null
) : IRequest<PagedResult<ReturnableReceiptItemDto>>;
