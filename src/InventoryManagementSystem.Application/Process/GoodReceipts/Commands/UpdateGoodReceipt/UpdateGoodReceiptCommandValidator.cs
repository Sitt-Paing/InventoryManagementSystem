using FluentValidation;

namespace InventoryManagementSystem.Application.Process.GoodReceipts.Commands.UpdateGoodReceipt;

public class UpdateGoodReceiptCommandValidator : AbstractValidator<UpdateGoodReceiptCommand>
{
    public UpdateGoodReceiptCommandValidator()
    {
        RuleFor(v => v.Id)
            .NotEmpty().WithMessage("Good Receipt ID is required.");

        RuleFor(v => v.ReceiptDate)
            .NotEmpty().WithMessage("Receipt Date is required.");

        RuleFor(v => v.ReceivedBy)
            .NotEmpty().WithMessage("Received By is required.")
            .MaximumLength(100).WithMessage("Received By cannot exceed 100 characters.");

        RuleFor(v => v.Note)
            .MaximumLength(500).WithMessage("Note cannot exceed 500 characters.");
    }
}
