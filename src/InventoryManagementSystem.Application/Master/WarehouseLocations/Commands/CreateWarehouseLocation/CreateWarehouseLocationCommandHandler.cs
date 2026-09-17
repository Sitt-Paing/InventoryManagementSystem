using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Master.WarehouseLocations.DTOs;
using InventoryManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Application.Master.WarehouseLocations.Commands.CreateWarehouseLocation;

public class CreateWarehouseLocationCommandHandler : IRequestHandler<CreateWarehouseLocationCommand, WarehouseLocationDto>
{
    private readonly IApplicationDbContext _context;

    public CreateWarehouseLocationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WarehouseLocationDto> Handle(CreateWarehouseLocationCommand command, CancellationToken cancellationToken)
    {
        var warehouse = await _context.Warehouses
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == command.WarehouseId && !w.DeletedOn.HasValue, cancellationToken);

        var entity = new WarehouseLocation
        {
            WarehouseId = command.WarehouseId,
            LocationCode = command.LocationCode,
            Zone = command.Zone,
            Rack = command.Rack,
            Bin = command.Bin,
            Barcode = command.Barcode,
            Capacity = command.Capacity,
            Status = command.Status
        };

        _context.WarehouseLocations.Add(entity);
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
            CreatedBy = entity.CreatedBy
        };
    }
}
