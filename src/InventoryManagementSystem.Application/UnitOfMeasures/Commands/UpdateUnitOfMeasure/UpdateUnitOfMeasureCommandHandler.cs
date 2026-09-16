using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.UnitOfMeasures.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Application.UnitOfMeasures.Commands.UpdateUnitOfMeasure;

public class UpdateUnitOfMeasureCommandHandler : IRequestHandler<UpdateUnitOfMeasureCommand, UnitOfMeasureDto?>
{
    private readonly IApplicationDbContext _context;

    public UpdateUnitOfMeasureCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UnitOfMeasureDto?> Handle(UpdateUnitOfMeasureCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.UnitOfMeasures
            .FirstOrDefaultAsync(u => u.Id == request.Id && !u.DeletedOn.HasValue, cancellationToken);

        if (entity == null) return null;

        var category = await _context.UomCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId && !c.DeletedOn.HasValue, cancellationToken);

        entity.CategoryId = request.CategoryId;
        entity.Code = request.Code.Trim().ToUpperInvariant();
        entity.Name = request.Name.Trim();
        entity.Symbol = request.Symbol?.Trim();
        entity.DecimalPlaces = request.DecimalPlaces;
        entity.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        return new UnitOfMeasureDto
        {
            Id = entity.Id,
            CategoryId = entity.CategoryId,
            CategoryName = category?.Name,
            Code = entity.Code,
            Name = entity.Name,
            Symbol = entity.Symbol,
            DecimalPlaces = entity.DecimalPlaces,
            IsActive = entity.IsActive,
            CreatedOn = entity.CreatedOn,
            CreatedBy = entity.CreatedBy,
            UpdatedOn = entity.UpdatedOn,
            UpdatedBy = entity.UpdatedBy,
            DeletedOn = entity.DeletedOn,
            DeletedBy = entity.DeletedBy
        };
    }
}
