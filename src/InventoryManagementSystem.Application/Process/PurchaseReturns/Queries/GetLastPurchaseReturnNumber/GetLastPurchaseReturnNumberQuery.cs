using MediatR;

namespace InventoryManagementSystem.Application.Process.PurchaseReturns.Queries.GetLastPurchaseReturnNumber;

public record GetLastPurchaseReturnNumberQuery(int Year) : IRequest<string?>;
