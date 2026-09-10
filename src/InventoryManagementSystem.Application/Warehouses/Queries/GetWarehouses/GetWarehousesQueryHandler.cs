using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Warehouses.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Application.Warehouses.Queries.GetWarehouses;

public class GetWarehousesQueryHandler : IRequestHandler<GetWarehousesQuery, List<WarehouseDto>>
{
    private readonly IApplicationDbContext _context;

    public GetWarehousesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<WarehouseDto>> Handle(GetWarehousesQuery request, CancellationToken cancellationToken)
    {
        return await _context.Warehouses
            .AsNoTracking()
            .Where(x => !x.DeletedOn.HasValue)
            .Select(w => new WarehouseDto
            {
                Id = w.Id,
                WarehouseCode = w.WarehouseCode,
                Name = w.Name,
                Address = w.Address,
                ContactPerson = w.ContactPerson,
                Phone = w.Phone,
                Email = w.Email,
                Capacity = w.Capacity,
                Status = w.Status,
                CreatedOn = w.CreatedOn,
                CreatedBy = w.CreatedBy,
                UpdatedOn = w.UpdatedOn,
                UpdatedBy = w.UpdatedBy,
                DeletedOn = w.DeletedOn,
                DeletedBy = w.DeletedBy
            })
            .ToListAsync(cancellationToken);
    }
}
