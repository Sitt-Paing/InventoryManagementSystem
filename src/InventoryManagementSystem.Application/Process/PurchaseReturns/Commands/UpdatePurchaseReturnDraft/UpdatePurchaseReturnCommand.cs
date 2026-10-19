using InventoryManagementSystem.Application.Process.PurchaseReturns.Commands.CreatePurchaseReturnDraft;
using InventoryManagementSystem.Application.Process.PurchaseReturns.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Process.PurchaseReturns.Commands.UpdatePurchaseReturnDraft;

public record UpdatePurchaseReturnCommand(
    Guid Id,
    int SupplierId,
    DateTime ReturnDate,
    string? ReturnReason,
    List<CreatePurchaseReturnItemInput> Items
) : IRequest<PurchaseReturnDto>;
