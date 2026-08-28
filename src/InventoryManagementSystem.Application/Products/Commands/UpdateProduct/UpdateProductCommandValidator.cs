using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Products.Commands.UpdateProduct;

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(v => v.Id)
            .NotEmpty()
            .WithMessage("Valid Product Id is required.");
        RuleFor(v => v.CategoryId)
            .GreaterThan(0)
            .WithMessage("Valid Category Id is required.");
        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Product Name is required.")
            .MaximumLength(250).WithMessage("Product Name must not exceed 250 characters.");
        RuleFor(v => v.SellingPrice)
            .GreaterThanOrEqualTo(0).When(x => x.UnitPrice == null).WithMessage("Selling Price must be greater than or equal to 0.");
        RuleFor(v => v.CostPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Cost Price must be greater than or equal to 0.");
        RuleFor(v => v.Barcode)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Barcode))
            .WithMessage("Barcode must not exceed 100 characters.");
        RuleFor(v => v.Sku)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.Sku))
            .WithMessage("SKU must not exceed 50 characters.");
        RuleFor(v => v.Brand)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Brand))
            .WithMessage("Brand must not exceed 100 characters.");
        RuleFor(v => v.Unit)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.Unit))
            .WithMessage("Unit must not exceed 50 characters.");
        RuleFor(v => v.Description)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Description))
            .WithMessage("Description must not exceed 500 characters.");
        RuleFor(v => v.ReorderLevel)
            .GreaterThanOrEqualTo(0).WithMessage("Reorder Level cannot be negative.");
        RuleFor(v => v.ReorderQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Reorder Quantity cannot be negative.");
        RuleFor(v => v.Tax)
            .GreaterThanOrEqualTo(0).WithMessage("Tax cannot be negative.");
        RuleFor(v => v.CurrentStock)
            .GreaterThanOrEqualTo(0).WithMessage("Current Stock cannot be negative.");
    }
}