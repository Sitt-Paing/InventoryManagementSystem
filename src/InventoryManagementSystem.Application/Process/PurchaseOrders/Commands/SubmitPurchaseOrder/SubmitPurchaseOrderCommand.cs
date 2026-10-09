using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Process.PurchaseOrders.Commands.SubmitPurchaseOrder;

public record SubmitPurchaseOrderCommand(Guid Id) : IRequest<bool>;