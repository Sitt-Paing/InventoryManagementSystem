using System;
using InventoryManagementSystem.Application.Process.GoodReceipts.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Process.GoodReceipts.Commands.UpdateGoodReceipt;

public record UpdateGoodReceiptCommand(
    Guid Id,
    DateTime ReceiptDate,
    bool Status,
    string ReceivedBy,
    string? Note
) : IRequest<GoodReceiptDto?>;
