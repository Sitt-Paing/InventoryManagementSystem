using FluentValidation;

namespace InventoryManagementSystem.Application.Process.GoodReceipts.Commands.CreateGoodReceipt;

public class CreateGoodReceiptCommandValidator : AbstractValidator<CreateGoodReceiptCommand>
{
    public CreateGoodReceiptCommandValidator()
    {
        RuleFor(v => v.ReceiptNo)
            .NotEmpty().WithMessage("Receipt Number is required.")
            .MaximumLength(50).WithMessage("Receipt Number cannot exceed 50 characters.");

        RuleFor(v => v.WarehouseId)
            .GreaterThan(0).WithMessage("A valid Warehouse must be selected.");

        RuleFor(v => v.PurchaseOrderId)
            .NotEmpty().WithMessage("Purchase Order is required.");

        RuleFor(v => v.SupplierId)
            .GreaterThan(0).WithMessage("A valid Supplier must be selected.");

        RuleFor(v => v.ReceiptDate)
            .NotEmpty().WithMessage("Receipt Date is required.");

        RuleFor(v => v.ReceivedBy)
            .NotEmpty().WithMessage("Received By is required.")
            .MaximumLength(100).WithMessage("Received By cannot exceed 100 characters.");

        RuleFor(v => v.Note)
            .MaximumLength(500).WithMessage("Note cannot exceed 500 characters.");

        RuleFor(v => v.Items)
            .NotEmpty().WithMessage("At least one receipt item is required.");

        RuleForEach(v => v.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .NotEmpty().WithMessage("Product is required for each item.");

            item.RuleFor(i => i.PurchaseOrderItemId)
                .GreaterThan(0).WithMessage("Purchase Order Item is required.");

            item.RuleFor(i => i.ReceivedQuantity)
                .GreaterThan(0).WithMessage("Received Quantity must be greater than 0.");

            item.RuleFor(i => i.UomId)
                .GreaterThan(0).WithMessage("UOM is required for each item.");
        });
    }
}
