using FluentValidation;

namespace InventoryManagementSystem.Application.Master.Departments.Commands.UpdateDepartment;

public class UpdateDepartmentCommandValidator : AbstractValidator<UpdateDepartmentCommand>
{
    public UpdateDepartmentCommandValidator()
    {
        RuleFor(x => x.id).GreaterThan(0).WithMessage("Department ID must be greater than 0.");
        RuleFor(x => x.deptName)
            .NotEmpty().WithMessage("Department name is required.")
            .MaximumLength(200).WithMessage("Department name cannot exceed 200 characters.");
    }
}
