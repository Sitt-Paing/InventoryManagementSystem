using FluentValidation;

namespace InventoryManagementSystem.Application.Process.PurchaseOrders.Commands.CreatePurchaseOrder;

public class CreatePurchaseOrderCommandValidator : AbstractValidator<CreatePurchaseOrderCommand>
{
    public CreatePurchaseOrderCommandValidator()
    {
        RuleFor(v => v.PurchaseOrderNo)
            .NotEmpty().WithMessage("Purchase Order Number is required.")
            .MaximumLength(50).WithMessage("Purchase Order Number cannot exceed 50 characters.");

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

        RuleForEach(v => v.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .NotEmpty().WithMessage("Product is required for each item.");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than 0.");

            item.RuleFor(i => i.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Unit Price must be 0 or greater.");

            item.RuleFor(i => i.UomId)
                .GreaterThan(0).WithMessage("UOM is required for each item.");
        });
    }
}
