using System;
using InventoryManagementSystem.Application.Process.GoodReceipts.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Process.GoodReceipts.Queries.GetGoodReceiptById;

public record GetGoodReceiptByIdQuery(Guid Id) : IRequest<GoodReceiptDto?>;
