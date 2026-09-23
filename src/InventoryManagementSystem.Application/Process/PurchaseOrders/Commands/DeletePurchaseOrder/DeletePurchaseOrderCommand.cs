using System;
using InventoryManagementSystem.Application.Process.PurchaseOrders.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Process.PurchaseOrders.Commands.DeletePurchaseOrder;

public record DeletePurchaseOrderCommand(Guid Id) : IRequest<PurchaseOrderDto?>;
