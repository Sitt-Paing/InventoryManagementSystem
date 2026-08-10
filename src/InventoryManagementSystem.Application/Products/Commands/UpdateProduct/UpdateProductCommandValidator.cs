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
            .NotEmpty()
            .WithMessage("Valid Category Id is required.");
        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Product Name is required.")
            .MaximumLength(250).WithMessage("Product Name must not exceed 250 characters.");
        RuleFor(v => v.UnitPrice)
            .GreaterThan(0).WithMessage("UnitPrice must be greater than 0");
        RuleFor(v => v.Sku)
            .NotEmpty()
            .WithMessage("SKU is required.");
        RuleFor(v => v.ReorderLevel)
            .GreaterThanOrEqualTo(0).WithMessage("ReorderLevel cannot be negative.");
        RuleFor(v => v.CurrentStock)
            .GreaterThanOrEqualTo(0).WithMessage("CurrentStock cannot be negative.");
    }
}