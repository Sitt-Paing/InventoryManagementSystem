using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Suppliers.Commands.UpdateSupplier;

public class UpdateSupplierCommandValidator : AbstractValidator<UpdateSupplierCommand>
{
    public UpdateSupplierCommandValidator()
    {
        RuleFor(x => x.id).GreaterThan(0).WithMessage("Supplier ID must be greater than 0.");
        RuleFor(x => x.supplierCode).NotEmpty().WithMessage("Supplier code is required.");
        RuleFor(x => x.companyName).NotEmpty().WithMessage("Company name is required.");
        RuleFor(x => x.paymentTerms).NotEmpty().WithMessage("Payment terms are required.");
        RuleFor(x => x.creditLimit).GreaterThanOrEqualTo(0).WithMessage("Credit limit must be greater than or equal to 0.");
    }
}
