using FluentValidation;

namespace InventoryManagementSystem.Application.Process.PurchaseReturns.Commands;

public class CreatePurchaseReturnCommandValidator : AbstractValidator<CreatePurchaseReturnCommand>
{
    public CreatePurchaseReturnCommandValidator()
    {
        RuleFor(v => v.ReturnNo)
            .NotEmpty().WithMessage("Return Number is required.")
            .MaximumLength(50).WithMessage("Return Number cannot exceed 50 characters.");

        RuleFor(v => v.SupplierId)
            .GreaterThan(0).WithMessage("A valid Supplier must be selected.");

        RuleFor(v => v.ReturnDate)
            .NotEmpty().WithMessage("Return Date is required.");

        RuleFor(v => v.ReturnReason)
            .MaximumLength(500).WithMessage("Return Reason cannot exceed 500 characters.");

        RuleFor(v => v.IdempotencyKey)
            .NotEmpty().WithMessage("Idempotency key is required.");

        RuleFor(v => v.Items)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("At least one return item is required.")
            .Must(items => items.Where(item => item is not null)
                .Select(item => item.GoodReceiptItemId).Distinct().Count()
                == items.Count(item => item is not null))
            .WithMessage("Each goods receipt item can appear only once in a return.");

        RuleForEach(v => v.Items)
            .NotNull().WithMessage("Return items cannot be null.")
            .SetValidator(new CreatePurchaseReturnItemInputValidator());
    }
}

public class CreatePurchaseReturnItemInputValidator : AbstractValidator<CreatePurchaseReturnItemInput>
{
    public CreatePurchaseReturnItemInputValidator()
    {
        RuleFor(v => v.GoodReceiptItemId)
            .GreaterThan(0).WithMessage("A valid Goods Receipt Item must be selected.");

        RuleFor(v => v.ReturnQuantity)
            .GreaterThan(0).WithMessage("Return Quantity must be greater than 0.")
            .PrecisionScale(18, 4, true)
            .WithMessage("Return Quantity must fit decimal(18,4), with at most 4 decimal places.");
    }
}
