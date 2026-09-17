using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Master.UnitOfMeasures.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Application.Master.UnitOfMeasures.Queries.GetUnitOfMeasureById;

public class GetUnitOfMeasureByIdQueryHandler : IRequestHandler<GetUnitOfMeasureByIdQuery, UnitOfMeasureDto?>
{
    private readonly IApplicationDbContext _context;

    public GetUnitOfMeasureByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UnitOfMeasureDto?> Handle(GetUnitOfMeasureByIdQuery request, CancellationToken cancellationToken)
    {
        var u = await _context.UnitOfMeasures
            .AsNoTracking()
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.DeletedOn.HasValue, cancellationToken);

        if (u == null) return null;

        return new UnitOfMeasureDto
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
        };
    }
}
