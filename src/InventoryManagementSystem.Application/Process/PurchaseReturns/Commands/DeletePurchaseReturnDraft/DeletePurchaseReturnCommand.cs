using MediatR;

namespace InventoryManagementSystem.Application.Process.PurchaseReturns.Commands.DeletePurchaseReturnDraft;

public record DeletePurchaseReturnCommand(Guid Id) : IRequest;
