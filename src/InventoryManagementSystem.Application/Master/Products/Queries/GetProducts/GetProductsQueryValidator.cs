using FluentValidation;

namespace InventoryManagementSystem.Application.Master.Products.Queries.GetProducts;

public class GetProductsQueryValidator : AbstractValidator<GetProductsQuery>
{
    public GetProductsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0.");
        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0.")
            .LessThanOrEqualTo(100).WithMessage("Page size must be less than or equal to 100.");
        RuleFor(x => x)
            .Must(x => x.PageNumber.HasValue == x.PageSize.HasValue)
            .WithMessage("Page number and page size must be provided together.");
    }
}
