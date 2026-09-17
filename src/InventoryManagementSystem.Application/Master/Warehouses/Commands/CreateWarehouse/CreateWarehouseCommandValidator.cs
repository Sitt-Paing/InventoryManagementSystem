using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Master.Warehouses.Commands.CreateWarehouse;

public class CreateWarehouseCommandValidator : AbstractValidator<CreateWarehouseCommand>
{
    public CreateWarehouseCommandValidator()
    {
        RuleFor(x => x.WarehouseCode).NotEmpty().WithMessage("WarehouseCode is required!")
            .MaximumLength(50).WithMessage("Warehouse code cannot exceed 50 characters.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Warehouse Name is required!")
            .MaximumLength(200).WithMessage("Warehouse name cannot exceed 50 characters.");
        RuleFor(x => x.ContactPerson)
             .MaximumLength(100).WithMessage("Contact person cannot exceed 100 characters.");
        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters.");
        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email address format.")
            .MaximumLength(100).WithMessage("Email address cannot exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Address)
            .MaximumLength(250).WithMessage("Address cannot exceed 250 characters.");
    }
}
