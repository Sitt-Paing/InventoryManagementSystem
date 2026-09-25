using System;
using System.Collections.Generic;
using InventoryManagementSystem.Application.Process.PurchaseOrders.DTOs;
using InventoryManagementSystem.Domain.Enums;
using MediatR;

namespace InventoryManagementSystem.Application.Process.PurchaseOrders.Commands.CreatePurchaseOrder;

public record CreatePurchaseOrderItemInput(
    Guid ProductId,
    decimal Quantity,
    decimal UnitPrice,
    long UomId,
    decimal ReceivedQuantity = 0
);

public record CreatePurchaseOrderCommand(
    string PurchaseOrderNo,
    int SupplierId,
    int WarehouseId,
    DateTime OrderDate,
    DateTime ExpectedDate,
    PurchaseOrderStatus Status,
    List<CreatePurchaseOrderItemInput> Items
) : IRequest<PurchaseOrderDto>;
