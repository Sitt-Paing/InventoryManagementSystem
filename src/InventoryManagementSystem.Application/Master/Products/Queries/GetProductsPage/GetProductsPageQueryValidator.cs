using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Master.Products.Queries.GetProductsPage;

public class GetProductsPageQueryValidator : AbstractValidator<GetProductsPageQuery>
{
    public GetProductsPageQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0.");
        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0.")
            .LessThanOrEqualTo(100).WithMessage("Page size must be less than or equal to 100.");
    }
}