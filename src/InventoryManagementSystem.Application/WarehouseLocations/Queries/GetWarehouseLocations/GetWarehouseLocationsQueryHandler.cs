using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.WarehouseLocations.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Application.WarehouseLocations.Queries.GetWarehouseLocations;

public class GetWarehouseLocationsQueryHandler : IRequestHandler<GetWarehouseLocationsQuery, List<WarehouseLocationDto>>
{
    private readonly IApplicationDbContext _context;

    public GetWarehouseLocationsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<WarehouseLocationDto>> Handle(GetWarehouseLocationsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.WarehouseLocations
            .AsNoTracking()
            .Include(x => x.Warehouse)
            .Where(x => !x.DeletedOn.HasValue);

        if (request.WarehouseId.HasValue && request.WarehouseId.Value > 0)
        {
            query = query.Where(x => x.WarehouseId == request.WarehouseId.Value);
        }

        return await query
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
            .ToListAsync(cancellationToken);
    }
}
