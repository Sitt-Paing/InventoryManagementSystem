using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Warehouses.Commands.UpdateWarehouse;

public class UpdateWarehouseValidator : AbstractValidator<UpdateWarehouseCommand>
{
    public UpdateWarehouseValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Warehouse ID must be greater than 0.");
        RuleFor(x => x.WarehouseCode).NotEmpty().WithMessage("Warehouse code is required.")
            .MaximumLength(50).WithMessage("Warehouse code cannot exceed 50 characters.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Warehouse name is required.")
            .MaximumLength(150).WithMessage("Warehouse name cannot exceed 150 characters.");
        RuleFor(x => x.ContactPerson)
            .MaximumLength(100).WithMessage("Contact person cannot exceed 100 characters.");
        RuleFor(x => x.Phone)
            .MaximumLength(50).WithMessage("Phone cannot exceed 50 characters.");
        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email address format.")
            .MaximumLength(100).WithMessage("Email address cannot exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Address)
            .MaximumLength(250).WithMessage("Address cannot exceed 250 characters.");
    }
}
