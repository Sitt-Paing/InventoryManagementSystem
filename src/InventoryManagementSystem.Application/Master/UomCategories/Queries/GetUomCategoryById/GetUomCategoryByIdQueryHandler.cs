using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Master.UomCategories.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Application.Master.UomCategories.Queries.GetUomCategoryById;

public class GetUomCategoryByIdQueryHandler : IRequestHandler<GetUomCategoryByIdQuery, UomCategoryDto?>
{
    private readonly IApplicationDbContext _context;

    public GetUomCategoryByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UomCategoryDto?> Handle(GetUomCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var category = await _context.UomCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.Id && !c.DeletedOn.HasValue, cancellationToken);

        if (category == null) return null;

        return new UomCategoryDto
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
