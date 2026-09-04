using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Suppliers.Commands.CreateSupplier;

public class  CreateSupplierCommandValidator : AbstractValidator<CreateSupplierCommand>
{
    public CreateSupplierCommandValidator()
    {
        RuleFor(x => x.supplierCode)
            .NotEmpty().WithMessage("Supplier code is required.")
            .MaximumLength(50).WithMessage("Supplier code cannot exceed 50 characters.");
        RuleFor(x => x.companyName)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(100).WithMessage("Company name cannot exceed 100 characters.");
        RuleFor(x => x.contactPerson)
            .MaximumLength(100).WithMessage("Contact person cannot exceed 100 characters.");
        RuleFor(x => x.phone)
            .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters.");
        RuleFor(x => x.email)
            .EmailAddress().WithMessage("Invalid email address format.")
            .MaximumLength(100).WithMessage("Email address cannot exceed 100 characters.");
        RuleFor(x => x.address)
            .MaximumLength(200).WithMessage("Address cannot exceed 200 characters.");
        RuleFor(x => x.paymentTerms)
            .NotEmpty().WithMessage("Payment terms are required.")
            .MaximumLength(50).WithMessage("Payment terms cannot exceed 50 characters.");
        RuleFor(x => x.creditLimit)
            .GreaterThanOrEqualTo(0).WithMessage("Credit limit must be greater than or equal to zero.");
    }
}