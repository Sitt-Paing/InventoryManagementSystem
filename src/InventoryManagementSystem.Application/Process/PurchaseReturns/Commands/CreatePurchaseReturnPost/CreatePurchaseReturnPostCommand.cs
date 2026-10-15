using InventoryManagementSystem.Application.Process.PurchaseReturns.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Process.PurchaseReturns.Commands.CreatePurchaseReturnPost;

public record CreatePurchaseReturnPostCommand(Guid PurchaseReturnId,Guid IdempotencyKey) : IRequest<PurchaseReturnDto>;
