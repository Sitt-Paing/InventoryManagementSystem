using FluentValidation;

namespace InventoryManagementSystem.Application.Report.StockBalances.Queries.GetStockBalances;

public class GetStockBalancesQueryValidator : AbstractValidator<GetStockBalancesQuery>
{
    public GetStockBalancesQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0.");
        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0.")
            .LessThanOrEqualTo(100).WithMessage("Page size must be less than or equal to 100.");
        RuleFor(x => x)
            .Must(x => x.PageNumber.HasValue == x.PageSize.HasValue)
            .WithMessage("Page number and page size must be provided together.");
        RuleFor(x => x)
            .Must(x => !x.PageNumber.HasValue || !x.PageSize.HasValue
                || (long)(x.PageNumber.Value - 1) * x.PageSize.Value <= int.MaxValue)
            .WithMessage("Page number is too large.");
        RuleFor(x => x)
            .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue
                || x.StartDate.Value.Date <= x.EndDate.Value.Date)
            .WithMessage("Start date must not be after end date.");
    }
}
