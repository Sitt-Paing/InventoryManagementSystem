using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Master.WarehouseLocations.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Application.Master.WarehouseLocations.Queries.GetWarehouseLocationById;

public class GetWarehouseLocationByIdQueryHandler : IRequestHandler<GetWarehouseLocationByIdQuery, WarehouseLocationDto?>
{
    private readonly IApplicationDbContext _context;

    public GetWarehouseLocationByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WarehouseLocationDto?> Handle(GetWarehouseLocationByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.WarehouseLocations
            .AsNoTracking()
            .Include(x => x.Warehouse)
            .Where(x => !x.DeletedOn.HasValue && x.Id == request.Id)
            .Select(x => new WarehouseLocationDto
            {
                Id = x.Id,
                WarehouseId = x.WarehouseId,
                WarehouseName = x.Warehouse != null ? x.Warehouse.Name : null,
                WarehouseCode = x.Warehouse != null ? x.Warehouse.WarehouseCode : null,
                LocationCode = x.LocationCode,
                Zone = x.Zone,
                Rack = x.Rack,
                Bin = x.Bin,
                Barcode = x.Barcode,
                Capacity = x.Capacity,
                Status = x.Status,
                CreatedOn = x.CreatedOn,
                CreatedBy = x.CreatedBy,
                UpdatedOn = x.UpdatedOn,
                UpdatedBy = x.UpdatedBy,
                DeletedOn = x.DeletedOn,
                DeletedBy = x.DeletedBy
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
