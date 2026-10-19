using FluentValidation;
using InventoryManagementSystem.Application.Process.PurchaseReturns.Commands.CreatePurchaseReturnDraft;

namespace InventoryManagementSystem.Application.Process.PurchaseReturns.Commands.UpdatePurchaseReturnDraft;

public class UpdatePurchaseReturnCommandValidator : AbstractValidator<UpdatePurchaseReturnCommand>
{
    public UpdatePurchaseReturnCommandValidator()
    {
        RuleFor(v => v.SupplierId)
            .GreaterThan(0).WithMessage("A valid Supplier must be selected.");

        RuleFor(v => v.ReturnDate)
            .NotEmpty().WithMessage("Return Date is required.");

        RuleFor(v => v.ReturnReason)
            .MaximumLength(500).WithMessage("Return Reason cannot exceed 500 characters.");

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
