using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Master.WarehouseLocations.DTOs;
using InventoryManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Application.Master.WarehouseLocations.Commands.UpdateWarehouseLocation;

public class UpdateWarehouseLocationCommandHandler : IRequestHandler<UpdateWarehouseLocationCommand, WarehouseLocationDto?>
{
    private readonly IApplicationDbContext _context;

    public UpdateWarehouseLocationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WarehouseLocationDto?> Handle(UpdateWarehouseLocationCommand command, CancellationToken cancellationToken)
    {
        var entity = await _context.WarehouseLocations
            .Where(x => !x.DeletedOn.HasValue)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);

        if (entity == null) return null;

        var warehouse = await _context.Warehouses
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == command.WarehouseId && !w.DeletedOn.HasValue, cancellationToken);

        entity.WarehouseId = command.WarehouseId;
        entity.LocationCode = command.LocationCode;
        entity.Zone = command.Zone;
        entity.Rack = command.Rack;
        entity.Bin = command.Bin;
        entity.Barcode = command.Barcode;
        entity.Capacity = command.Capacity;
        entity.Status = command.Status;

        await _context.SaveChangesAsync(cancellationToken);

        return new WarehouseLocationDto
        {
            Id = entity.Id,
            WarehouseId = entity.WarehouseId,
            WarehouseName = warehouse?.Name,
            WarehouseCode = warehouse?.WarehouseCode,
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
            UpdatedBy = entity.UpdatedBy
        };
    }
}
