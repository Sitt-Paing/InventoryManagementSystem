using InventoryManagementSystem.Application.Process.PurchaseOrders.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Process.PurchaseOrders.Queries.GetPurchaseOrderEmailPreview;

public record GetPurchaseOrderEmailPreviewQuery(Guid PoId) : IRequest<PurchaseOrderEmailPreviewDto>;
