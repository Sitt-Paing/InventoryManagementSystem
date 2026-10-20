using FluentValidation;

namespace InventoryManagementSystem.Application.Process.StockIssues.Commands.CreateStockIssueDraft;

public class CreateStockIssueCommandValidator : AbstractValidator<CreateStockIssueCommand>
{
    public CreateStockIssueCommandValidator()
    {
        RuleFor(v => v.IssueNo)
            .NotEmpty().WithMessage("Issue Number is required.")
            .MaximumLength(50).WithMessage("Issue Number cannot exceed 50 characters.");

        RuleFor(v => v.IssueDate)
            .NotEmpty().WithMessage("Issue Date is required.");

        RuleFor(v => v.WarehouseId)
            .GreaterThan(0).WithMessage("A valid Warehouse must be selected.");

        RuleFor(v => v.DepartmentId)
            .GreaterThan(0).WithMessage("A valid Department must be selected.");

        RuleFor(v => v.ReceivedBy)
            .NotEmpty().WithMessage("Received By is required.")
            .MaximumLength(100).WithMessage("Received By cannot exceed 100 characters.");

        RuleFor(v => v.Note)
            .MaximumLength(500).WithMessage("Note cannot exceed 500 characters.");

        RuleFor(v => v.Items)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("At least one issue item is required.")
            .Must(items => items.Where(item => item is not null)
                .Select(item => item.ProductId).Distinct().Count()
                == items.Count(item => item is not null))
            .WithMessage("Each product can appear only once in an issue.");

        RuleForEach(v => v.Items)
            .NotNull().WithMessage("Issue items cannot be null.")
            .SetValidator(new CreateStockIssueItemInputValidator());
    }
}

public class CreateStockIssueItemInputValidator : AbstractValidator<CreateStockIssueItemInput>
{
    public CreateStockIssueItemInputValidator()
    {
        RuleFor(v => v.ProductId)
            .NotEmpty().WithMessage("A valid Product must be selected.");

        RuleFor(v => v.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0.")
            .PrecisionScale(18, 4, true)
            .WithMessage("Quantity must fit decimal(18,4), with at most 4 decimal places.");
    }
}
