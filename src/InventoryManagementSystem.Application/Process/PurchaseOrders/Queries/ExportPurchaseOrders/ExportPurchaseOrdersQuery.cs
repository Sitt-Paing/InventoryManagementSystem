using System;
using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Domain.Enums;
using MediatR;

namespace InventoryManagementSystem.Application.Process.PurchaseOrders.Queries.ExportPurchaseOrders;

public record ExportPurchaseOrdersQuery(
    string? Q = null,
    DateTime? OrderDate = null,
    int? SupplierId = null,
    int? WarehouseId = null,
    PurchaseOrderStatus? Status = null,
    string Format = "excel",
    string FontName = "Pyidaungsu"
) : IRequest<ExportFileDto>;
