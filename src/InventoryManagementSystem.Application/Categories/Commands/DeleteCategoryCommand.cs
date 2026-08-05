using InventoryManagementSystem.Application.Categories.DTOs;
using InventoryManagementSystem.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Categories.Commands;

public record DeleteCategoryCommand(long Id) : IRequest<CategoryDto?>;

public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, CategoryDto?>
{
    private readonly IApplicationDbContext _context;

    public DeleteCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CategoryDto?> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Categories
            .Where(c => !c.DeletedOn.HasValue)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (entity == null) return null;

        var categoryDto = new CategoryDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive,
            CreatedOn = entity.CreatedOn,
            CreatedBy = entity.CreatedBy,
            UpdatedOn = entity.UpdatedOn,
            UpdatedBy = entity.UpdatedBy,
            DeletedOn = entity.DeletedOn,
            DeletedBy = entity.DeletedBy
        };

        _context.Categories.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return categoryDto;
    }
}
