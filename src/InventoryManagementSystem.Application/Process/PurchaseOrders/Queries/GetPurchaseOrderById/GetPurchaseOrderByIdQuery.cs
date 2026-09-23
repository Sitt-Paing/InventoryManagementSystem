using System;
using InventoryManagementSystem.Application.Process.PurchaseOrders.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Process.PurchaseOrders.Queries.GetPurchaseOrderById;

public record GetPurchaseOrderByIdQuery(Guid Id) : IRequest<PurchaseOrderDto?>;
