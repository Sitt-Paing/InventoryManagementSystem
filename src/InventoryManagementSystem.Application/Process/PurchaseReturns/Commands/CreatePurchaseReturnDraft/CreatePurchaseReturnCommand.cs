using MediatR;
using System;
using System.Collections.Generic;
using InventoryManagementSystem.Application.Process.PurchaseReturns.DTOs;

namespace InventoryManagementSystem.Application.Process.PurchaseReturns.Commands.CreatePurchaseReturnDraft;

public record CreatePurchaseReturnItemInput(
    long GoodReceiptItemId,
    decimal ReturnQuantity
);

public record CreatePurchaseReturnCommand(
    string ReturnNo,
    int SupplierId,
    DateTime ReturnDate,
    string? ReturnReason,
    Guid IdempotencyKey,
    List<CreatePurchaseReturnItemInput> Items
) : IRequest<PurchaseReturnDto>;
