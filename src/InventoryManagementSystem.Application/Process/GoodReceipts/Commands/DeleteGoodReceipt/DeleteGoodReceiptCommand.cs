using System;
using InventoryManagementSystem.Application.Process.GoodReceipts.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Process.GoodReceipts.Commands.DeleteGoodReceipt;

public record DeleteGoodReceiptCommand(Guid Id) : IRequest<GoodReceiptDto?>;
