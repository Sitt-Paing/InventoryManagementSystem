using FluentValidation;

namespace InventoryManagementSystem.Application.UnitOfMeasures.Commands.UpdateUnitOfMeasure;

public class UpdateUnitOfMeasureCommandValidator : AbstractValidator<UpdateUnitOfMeasureCommand>
{
    public UpdateUnitOfMeasureCommandValidator()
    {
        RuleFor(v => v.Id)
            .GreaterThan(0).WithMessage("Valid UOM Id is required.");

        RuleFor(v => v.CategoryId)
            .GreaterThan(0).WithMessage("Category is required.");

        RuleFor(v => v.Code)
            .NotEmpty().WithMessage("UOM Code is required.")
            .MaximumLength(20).WithMessage("UOM Code must not exceed 20 characters.");

        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("UOM Name is required.")
            .MaximumLength(150).WithMessage("UOM Name must not exceed 150 characters.");

        RuleFor(v => v.Symbol)
            .MaximumLength(20).WithMessage("Symbol must not exceed 20 characters.");

        RuleFor(v => v.DecimalPlaces)
            .InclusiveBetween(0, 4).WithMessage("Decimal places must be between 0 and 4.");
    }
}
