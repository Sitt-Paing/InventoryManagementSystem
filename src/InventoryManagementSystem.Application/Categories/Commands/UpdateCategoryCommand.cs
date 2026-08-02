using FluentValidation;
using InventoryManagementSystem.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Categories.Commands;

public record UpdateCategoryCommand(long Id, string Name, string? Description, bool IsActive) : IRequest<bool>;

public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(v => v.Id)
            .GreaterThan(0).WithMessage("Valid Category Id is required.");

        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Category Name is required.")
            .MaximumLength(250).WithMessage("Category Name must not exceed 250 characters.");

        RuleFor(v => v.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (entity == null) return false;

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
