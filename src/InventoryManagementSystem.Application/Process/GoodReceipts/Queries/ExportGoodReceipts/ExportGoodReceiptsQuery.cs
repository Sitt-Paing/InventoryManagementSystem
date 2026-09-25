using System;
using InventoryManagementSystem.Application.Common.Models;
using MediatR;

namespace InventoryManagementSystem.Application.Process.GoodReceipts.Queries.ExportGoodReceipts;

public record ExportGoodReceiptsQuery(
    string? Q = null,
    DateTime? ReceiptDate = null,
    int? SupplierId = null,
    int? WarehouseId = null,
    Guid? PurchaseOrderId = null,
    bool? Status = null,
    string Format = "excel",
    string FontName = "Pyidaungsu"
) : IRequest<ExportFileDto>;
