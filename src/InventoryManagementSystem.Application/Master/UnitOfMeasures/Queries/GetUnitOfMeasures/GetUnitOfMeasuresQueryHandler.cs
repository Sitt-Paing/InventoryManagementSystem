using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Master.UnitOfMeasures.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Application.Master.UnitOfMeasures.Queries.GetUnitOfMeasures;

public class GetUnitOfMeasuresQueryHandler : IRequestHandler<GetUnitOfMeasuresQuery, List<UnitOfMeasureDto>>
{
    private readonly IApplicationDbContext _context;

    public GetUnitOfMeasuresQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<UnitOfMeasureDto>> Handle(GetUnitOfMeasuresQuery request, CancellationToken cancellationToken)
    {
        var query = _context.UnitOfMeasures
            .AsNoTracking()
            .Include(u => u.Category)
            .Where(u => !u.DeletedOn.HasValue);

        if (request.CategoryId.HasValue && request.CategoryId.Value > 0)
        {
            query = query.Where(u => u.CategoryId == request.CategoryId.Value);
        }

        return await query
            .Select(u => new UnitOfMeasureDto
            {
                Id = u.Id,
                CategoryId = u.CategoryId,
                CategoryName = u.Category != null ? u.Category.Name : null,
                Code = u.Code,
                Name = u.Name,
                Symbol = u.Symbol,
                DecimalPlaces = u.DecimalPlaces,
                IsActive = u.IsActive,
                CreatedOn = u.CreatedOn,
                CreatedBy = u.CreatedBy,
                UpdatedOn = u.UpdatedOn,
                UpdatedBy = u.UpdatedBy,
                DeletedOn = u.DeletedOn,
                DeletedBy = u.DeletedBy
            })
            .ToListAsync(cancellationToken);
    }
}
