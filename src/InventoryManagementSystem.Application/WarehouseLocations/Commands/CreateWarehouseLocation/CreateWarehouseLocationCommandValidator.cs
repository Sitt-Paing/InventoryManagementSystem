using FluentValidation;
using InventoryManagementSystem.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.WarehouseLocations.Commands.CreateWarehouseLocation;

public class CreateWarehouseLocationCommandValidator : AbstractValidator<CreateWarehouseLocationCommand>
{
    public CreateWarehouseLocationCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.WarehouseId)
            .GreaterThan(0).WithMessage("Warehouse is required.")
            .MustAsync(async (warehouseId, cancellationToken) =>
                await context.Warehouses.AnyAsync(w => w.Id == warehouseId && !w.DeletedOn.HasValue, cancellationToken))
            .WithMessage("Selected Warehouse does not exist.");

        RuleFor(x => x.LocationCode)
            .NotEmpty().WithMessage("Location code is required.")
            .MaximumLength(50).WithMessage("Location code cannot exceed 50 characters.");

        RuleFor(x => x.Zone)
            .MaximumLength(50).WithMessage("Zone cannot exceed 50 characters.");

        RuleFor(x => x.Rack)
            .MaximumLength(50).WithMessage("Rack cannot exceed 50 characters.");

        RuleFor(x => x.Bin)
            .MaximumLength(50).WithMessage("Bin cannot exceed 50 characters.");

        RuleFor(x => x.Barcode)
            .MaximumLength(100).WithMessage("Barcode cannot exceed 100 characters.");

        RuleFor(x => x.Capacity)
            .GreaterThanOrEqualTo(0).When(x => x.Capacity.HasValue)
            .WithMessage("Capacity cannot be negative.");
    }
}
