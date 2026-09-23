using System;
using System.Collections.Generic;
using InventoryManagementSystem.Application.Process.PurchaseOrders.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Process.PurchaseOrders.Commands.UpdatePurchaseOrder;

public record UpdatePurchaseOrderItemInput(
    long? Id,
    Guid ProductId,
    decimal Quantity,
    decimal UnitPrice,
    long UomId,
    decimal ReceivedQuantity = 0
);

public record UpdatePurchaseOrderCommand(
    Guid Id,
    string PurchaseOrderNo,
    int SupplierId,
    int WarehouseId,
    DateTime OrderDate,
    DateTime ExpectedDate,
    bool Status,
    List<UpdatePurchaseOrderItemInput> Items
) : IRequest<PurchaseOrderDto?>;
