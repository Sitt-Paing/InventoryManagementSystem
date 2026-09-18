using FluentValidation;

namespace InventoryManagementSystem.Application.Process.StockTransactions.Command.CreateStockTransactions;

public class CreateStockTransactionsCommandValidator : AbstractValidator<CreateStockTransactionsCommand>
{

    public CreateStockTransactionsCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product is required.");

        RuleFor(x => x.WarehouseId)
            .GreaterThan(0).WithMessage("Warehouse is required.");

        RuleFor(x => x.WarehouseLocationId)
            .GreaterThan(0).WithMessage("Warehouse Location is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0.");

        RuleFor(x => x.TransactionType)
            .NotEmpty().WithMessage("Transaction Type is required.")
            .Must(t => t == "IN" || t == "OUT" || t == "ADJUSTMENT")
            .WithMessage("Transaction Type must be either 'IN', 'OUT', or 'ADJUSTMENT'.");

        RuleFor(x => x.TransactionDate)
            .NotEmpty().WithMessage("Transaction Date is required.");

        RuleFor(x => x.Note)
            .MaximumLength(250).When(x => !string.IsNullOrWhiteSpace(x.Note))
            .WithMessage("Note must not exceed 250 characters.");
    }
}