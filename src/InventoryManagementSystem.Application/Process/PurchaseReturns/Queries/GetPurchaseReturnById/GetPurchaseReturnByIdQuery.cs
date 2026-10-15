using InventoryManagementSystem.Application.Process.PurchaseReturns.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Process.PurchaseReturns.Queries.GetPurchaseReturnById;

public record GetPurchaseReturnByIdQuery(Guid Id) : IRequest<PurchaseReturnDto>;
