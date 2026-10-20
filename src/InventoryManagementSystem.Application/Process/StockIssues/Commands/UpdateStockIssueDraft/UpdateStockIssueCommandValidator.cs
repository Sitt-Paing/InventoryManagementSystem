using FluentValidation;
using InventoryManagementSystem.Application.Process.StockIssues.Commands.CreateStockIssueDraft;

namespace InventoryManagementSystem.Application.Process.StockIssues.Commands.UpdateStockIssueDraft;

public class UpdateStockIssueCommandValidator : AbstractValidator<UpdateStockIssueCommand>
{
    public UpdateStockIssueCommandValidator()
    {
        RuleFor(v => v.Id).NotEmpty();
        RuleFor(v => v.IssueDate).NotEmpty().WithMessage("Issue Date is required.");
        RuleFor(v => v.WarehouseId).GreaterThan(0).WithMessage("Select a Warehouse.");
        RuleFor(v => v.DepartmentId).GreaterThan(0).WithMessage("Select a Department.");
        RuleFor(v => v.ReceivedBy).NotEmpty().MaximumLength(100);
        RuleFor(v => v.Note).MaximumLength(500);
        RuleFor(v => v.Items)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("At least one issue item is required.")
            .Must(items => items.Where(item => item is not null)
                .Select(item => item.ProductId).Distinct().Count() == items.Count(item => item is not null))
            .WithMessage("Each product can appear only once in an issue.");
        RuleForEach(v => v.Items).NotNull().SetValidator(new CreateStockIssueItemInputValidator());
    }
}
