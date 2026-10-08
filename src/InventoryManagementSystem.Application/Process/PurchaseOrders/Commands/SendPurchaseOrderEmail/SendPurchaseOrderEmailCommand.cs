using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Process.PurchaseOrders.Commands.SendPurchaseOrderEmail;

public record SendPurchaseOrderEmailCommand(Guid PoId, Guid IdempotencyKey) : IRequest<Guid>;
