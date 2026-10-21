using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.UserManage.SupplierAccounts.Commands.CreateSupplierAccount;
public class CreateSupplierAccountCommandValidator : AbstractValidator<CreateSupplierAccountCommand>
{
    public CreateSupplierAccountCommandValidator()
    {
        RuleFor(v => v.SupplierId)
            .GreaterThan(0);

        RuleFor(v => v.UserName)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(v => v.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(v => v.Password)
            .NotEmpty()
            .MinimumLength(6);
    }
}
