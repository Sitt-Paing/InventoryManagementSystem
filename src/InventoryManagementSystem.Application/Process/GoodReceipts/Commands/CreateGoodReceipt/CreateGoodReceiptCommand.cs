using System;
using System.Collections.Generic;
using InventoryManagementSystem.Application.Process.GoodReceipts.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Process.GoodReceipts.Commands.CreateGoodReceipt;

public record CreateGoodReceiptItemInput(
    long PurchaseOrderItemId,
    Guid ProductId,
    long UomId,
    decimal ReceivedQuantity
);

public record CreateGoodReceiptCommand(
    string ReceiptNo,
    int WarehouseId,
    Guid PurchaseOrderId,
    int SupplierId,
    DateTime ReceiptDate,
    bool Status,
    string ReceivedBy,
    string? Note,
    List<CreateGoodReceiptItemInput> Items
) : IRequest<GoodReceiptDto>;
