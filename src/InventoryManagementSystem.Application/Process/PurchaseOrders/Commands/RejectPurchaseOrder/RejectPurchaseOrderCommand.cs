using FluentValidation;
using MediatR;

namespace InventoryManagementSystem.Application.Process.PurchaseOrders.Commands.RejectPurchaseOrder;

public record RejectPurchaseOrderCommand(Guid Id, string Reason) : IRequest<bool>;

public class RejectPurchaseOrderCommandValidator : AbstractValidator<RejectPurchaseOrderCommand>
{
    public RejectPurchaseOrderCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Reason)
            .Must(reason => !string.IsNullOrWhiteSpace(reason) && reason.Trim().Length <= 500)
            .WithMessage("A rejection reason of 1 to 500 characters is required.");
    }
}
