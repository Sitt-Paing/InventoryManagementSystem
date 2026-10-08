using System;
using System.Collections.Generic;
using InventoryManagementSystem.Application.Process.PurchaseOrders.DTOs;
using InventoryManagementSystem.Domain.Enums;
using MediatR;

namespace InventoryManagementSystem.Application.Process.PurchaseOrders.Commands.UpdatePurchaseOrder;

public record UpdatePurchaseOrderItemInput(
    long? Id,
    Guid ProductId,
    decimal Quantity,
    decimal UnitPrice,
    long UomId
);

public record UpdatePurchaseOrderCommand(
    Guid Id,
    int SupplierId,
    int WarehouseId,
    DateTime OrderDate,
    DateTime ExpectedDate,
    List<UpdatePurchaseOrderItemInput> Items
) : IRequest<PurchaseOrderDto?>;
