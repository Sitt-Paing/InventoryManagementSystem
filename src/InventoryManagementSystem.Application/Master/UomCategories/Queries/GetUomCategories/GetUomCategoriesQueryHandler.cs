using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Master.UomCategories.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Application.Master.UomCategories.Queries.GetUomCategories;

public class GetUomCategoriesQueryHandler : IRequestHandler<GetUomCategoriesQuery, List<UomCategoryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetUomCategoriesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<UomCategoryDto>> Handle(GetUomCategoriesQuery request, CancellationToken cancellationToken)
    {
        return await _context.UomCategories
            .AsNoTracking()
            .Where(c => !c.DeletedOn.HasValue)
            .Select(c => new UomCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IsActive = c.IsActive,
                CreatedOn = c.CreatedOn,
                CreatedBy = c.CreatedBy,
                UpdatedOn = c.UpdatedOn,
                UpdatedBy = c.UpdatedBy,
                DeletedOn = c.DeletedOn,
                DeletedBy = c.DeletedBy
            })
            .ToListAsync(cancellationToken);
    }
}
