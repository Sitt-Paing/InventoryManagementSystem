using FluentValidation;

namespace InventoryManagementSystem.Application.Master.UomCategories.Commands.CreateUomCategory;

public class CreateUomCategoryCommandValidator : AbstractValidator<CreateUomCategoryCommand>
{
    public CreateUomCategoryCommandValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("UOM Category Name is required.")
            .MaximumLength(100).WithMessage("UOM Category Name must not exceed 100 characters.");

        RuleFor(v => v.Description)
            .MaximumLength(250).WithMessage("Description must not exceed 250 characters.");
    }
}
