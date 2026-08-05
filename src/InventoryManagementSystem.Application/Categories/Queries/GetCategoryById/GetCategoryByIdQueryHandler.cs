using InventoryManagementSystem.Application.Categories.DTOs;
using InventoryManagementSystem.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Categories.Queries.GetCategoryById;

public class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, CategoryDto?>
{
    private readonly IApplicationDbContext _context;

    public GetCategoryByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CategoryDto?> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var category = await _context.Categories
            .AsNoTracking()
            .Where(c => !c.DeletedOn.HasValue)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (category == null) return null;

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive,
            CreatedOn = category.CreatedOn,
            CreatedBy = category.CreatedBy,
            UpdatedOn = category.UpdatedOn,
            UpdatedBy = category.UpdatedBy,
            DeletedOn = category.DeletedOn,
            DeletedBy = category.DeletedBy
        };
    }
}
