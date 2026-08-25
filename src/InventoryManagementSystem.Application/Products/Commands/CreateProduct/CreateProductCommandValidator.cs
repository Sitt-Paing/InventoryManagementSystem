using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Products.Commands.CreateProduct;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        //RuleFor(x => x.Id).NotEmpty().WithMessage("Product Id is required.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Product Name is required.");
        RuleFor(x => x.categoryId).GreaterThan(0).WithMessage("Category Id must be greater").NotEmpty().WithMessage("Category Id is required.");
        RuleFor(x => x.UnitPrice).GreaterThan(0).WithMessage("Unit Price must be greater than 0.").NotEmpty().WithMessage("Price is required.");
        RuleFor(x => x.Barcode).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.Barcode)).WithMessage("Barcode must not exceed 100 characters");
        RuleFor(x => x.CurrentStock).GreaterThanOrEqualTo(0).WithMessage("Current Stock must be greater than or equal to 0.").NotEmpty().WithMessage("Current Stock is required.");
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0).WithMessage("Reorder Level must be greater than or equal to 0.").NotEmpty().WithMessage("Reorder Level is required.");
    }
}