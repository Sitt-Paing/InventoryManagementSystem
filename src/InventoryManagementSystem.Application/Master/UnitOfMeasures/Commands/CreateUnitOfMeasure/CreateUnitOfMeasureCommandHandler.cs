using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Master.UnitOfMeasures.DTOs;
using InventoryManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Application.Master.UnitOfMeasures.Commands.CreateUnitOfMeasure;

public class CreateUnitOfMeasureCommandHandler : IRequestHandler<CreateUnitOfMeasureCommand, UnitOfMeasureDto>
{
    private readonly IApplicationDbContext _context;

    public CreateUnitOfMeasureCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UnitOfMeasureDto> Handle(CreateUnitOfMeasureCommand request, CancellationToken cancellationToken)
    {
        var category = await _context.UomCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId && !c.DeletedOn.HasValue, cancellationToken);

        var entity = new UnitOfMeasure
        {
            CategoryId = request.CategoryId,
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            Symbol = request.Symbol?.Trim(),
            DecimalPlaces = request.DecimalPlaces,
            IsActive = true
        };

        _context.UnitOfMeasures.Add(entity);
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
