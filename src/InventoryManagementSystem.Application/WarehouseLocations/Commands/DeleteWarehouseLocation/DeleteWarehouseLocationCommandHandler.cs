using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.WarehouseLocations.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Application.WarehouseLocations.Commands.DeleteWarehouseLocation;

public class DeleteWarehouseLocationCommandHandler : IRequestHandler<DeleteWarehouseLocationCommand, WarehouseLocationDto?>
{
    private readonly IApplicationDbContext _context;

    public DeleteWarehouseLocationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WarehouseLocationDto?> Handle(DeleteWarehouseLocationCommand command, CancellationToken cancellationToken)
    {
        var entity = await _context.WarehouseLocations
            .Include(x => x.Warehouse)
            .Where(x => !x.DeletedOn.HasValue)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);

        if (entity == null) return null;

        _context.WarehouseLocations.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return new WarehouseLocationDto
        {
            Id = entity.Id,
            WarehouseId = entity.WarehouseId,
            WarehouseName = entity.Warehouse?.Name,
            WarehouseCode = entity.Warehouse?.WarehouseCode,
            LocationCode = entity.LocationCode,
            Zone = entity.Zone,
            Rack = entity.Rack,
            Bin = entity.Bin,
            Barcode = entity.Barcode,
            Capacity = entity.Capacity,
            Status = entity.Status,
            CreatedOn = entity.CreatedOn,
            CreatedBy = entity.CreatedBy,
            UpdatedOn = entity.UpdatedOn,
            UpdatedBy = entity.UpdatedBy,
            DeletedOn = entity.DeletedOn,
            DeletedBy = entity.DeletedBy
        };
    }
}
