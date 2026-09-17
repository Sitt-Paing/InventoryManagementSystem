using FluentValidation;

namespace InventoryManagementSystem.Application.Master.ProductUomConversions.Commands.UpdateProductUomConversion;

public class UpdateProductUomConversionCommandValidator : AbstractValidator<UpdateProductUomConversionCommand>
{
    public UpdateProductUomConversionCommandValidator()
    {
        RuleFor(v => v.Id)
            .GreaterThan(0).WithMessage("Valid Id is required.");

        RuleFor(v => v.FromUomId)
            .GreaterThan(0).WithMessage("From UOM is required.");

        RuleFor(v => v.ToUomId)
            .GreaterThan(0).WithMessage("To UOM is required.");

        RuleFor(v => v.ConversionFactor)
            .GreaterThan(0).WithMessage("Conversion Factor must be greater than 0.");

        RuleFor(v => v.Barcode)
            .MaximumLength(50).WithMessage("Barcode must not exceed 50 characters.");
    }
}
