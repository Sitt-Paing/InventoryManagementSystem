using FluentValidation;

namespace InventoryManagementSystem.Application.UomCategories.Commands.UpdateUomCategory;

public class UpdateUomCategoryCommandValidator : AbstractValidator<UpdateUomCategoryCommand>
{
    public UpdateUomCategoryCommandValidator()
    {
        RuleFor(v => v.Id)
            .GreaterThan(0).WithMessage("Valid UOM Category Id is required.");

        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("UOM Category Name is required.")
            .MaximumLength(100).WithMessage("UOM Category Name must not exceed 100 characters.");

        RuleFor(v => v.Description)
            .MaximumLength(250).WithMessage("Description must not exceed 250 characters.");
    }
}
