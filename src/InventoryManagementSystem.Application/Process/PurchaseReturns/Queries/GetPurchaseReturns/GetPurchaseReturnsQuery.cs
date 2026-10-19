using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Process.PurchaseReturns.DTOs;
using InventoryManagementSystem.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Process.PurchaseReturns.Queries.GetPurchaseReturns;

public record GetPurchaseReturnsQuery(
    string? Q = null,
    int? SupplierId = null,
    PurchaseReturnStatus? Status = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    string? SortField = null,
    int Order = -1,
    int? PageNumber = null,
    int? PageSize = null
    ) : IRequest<PagedResult<PurchaseReturnDto>>;
