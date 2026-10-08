using FluentValidation;

namespace InventoryManagementSystem.Application.Process.PurchaseOrders.Commands.CreatePurchaseOrder;

public class CreatePurchaseOrderCommandValidator : AbstractValidator<CreatePurchaseOrderCommand>
{
    public CreatePurchaseOrderCommandValidator()
    {
        RuleFor(v => v.IdempotencyKey).NotEmpty();

        RuleFor(v => v.SupplierId)
            .GreaterThan(0).WithMessage("A valid Supplier must be selected.");

        RuleFor(v => v.WarehouseId)
            .GreaterThan(0).WithMessage("A valid Warehouse must be selected.");

        RuleFor(v => v.OrderDate)
            .NotEmpty().WithMessage("Order Date is required.");

        RuleFor(v => v.ExpectedDate)
            .NotEmpty().WithMessage("Expected Date is required.")
            .GreaterThanOrEqualTo(v => v.OrderDate).WithMessage("Expected Date must be on or after Order Date.");

        RuleFor(v => v.Items)
            .NotEmpty().WithMessage("At least one purchase order item is required.");
        RuleFor(v => v.Items).Must(items =>
        {
            if (items == null) return true;
            try { return items.Sum(i => i.Quantity * i.UnitPrice) <= 9999999999999999.99m; }
            catch (OverflowException) { return false; }
        }).WithMessage("The purchase order total exceeds the supported amount.");

        RuleForEach(v => v.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .NotEmpty().WithMessage("Product is required for each item.");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than 0.")
                .PrecisionScale(18, 4, true);

            item.RuleFor(i => i.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Unit Price must be 0 or greater.")
                .PrecisionScale(18, 2, true);

            item.RuleFor(i => i.UomId)
                .GreaterThan(0).WithMessage("UOM is required for each item.");
        });
    }
}
