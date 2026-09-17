using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Master.UnitOfMeasures.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Application.Master.UnitOfMeasures.Commands.DeleteUnitOfMeasure;

public class DeleteUnitOfMeasureCommandHandler : IRequestHandler<DeleteUnitOfMeasureCommand, UnitOfMeasureDto?>
{
    private readonly IApplicationDbContext _context;

    public DeleteUnitOfMeasureCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UnitOfMeasureDto?> Handle(DeleteUnitOfMeasureCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.UnitOfMeasures
            .Include(u => u.Category)
            .FirstOrDefaultAsync(u => u.Id == request.Id && !u.DeletedOn.HasValue, cancellationToken);

        if (entity == null) return null;

        var dto = new UnitOfMeasureDto
        {
            Id = entity.Id,
            CategoryId = entity.CategoryId,
            CategoryName = entity.Category != null ? entity.Category.Name : null,
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

        _context.UnitOfMeasures.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return dto;
    }
}
