using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Products.Commands.CreateProduct;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Product Name is required.").MaximumLength(250).WithMessage("Product Name must not exceed 250 characters.");
        RuleFor(x => x.CategoryId).GreaterThan(0).WithMessage("Category is required.");
        RuleFor(x => x.SellingPrice).GreaterThanOrEqualTo(0).WithMessage("Selling Price must be greater than or equal to 0.");
        RuleFor(x => x.CostPrice).GreaterThanOrEqualTo(0).WithMessage("Cost Price must be greater than or equal to 0.");
        RuleFor(x => x.Barcode).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.Barcode)).WithMessage("Barcode must not exceed 100 characters.");
        RuleFor(x => x.Sku).MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.Sku)).WithMessage("SKU must not exceed 50 characters.");
        RuleFor(x => x.Brand).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.Brand)).WithMessage("Brand must not exceed 100 characters.");
        RuleFor(x => x.Unit).MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.Unit)).WithMessage("Unit must not exceed 50 characters.");
        RuleFor(x => x.Description).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Description)).WithMessage("Description must not exceed 500 characters.");
        RuleFor(x => x.CurrentStock).GreaterThanOrEqualTo(0).WithMessage("Current Stock must be greater than or equal to 0.");
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0).WithMessage("Reorder Level must be greater than or equal to 0.");
        RuleFor(x => x.ReorderQuantity).GreaterThanOrEqualTo(0).WithMessage("Reorder Quantity must be greater than or equal to 0.");
        RuleFor(x => x.Tax).GreaterThanOrEqualTo(0).WithMessage("Tax must be greater than or equal to 0.");
    }
}