using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Master.Warehouses.DTOs;
using InventoryManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Application.Master.Warehouses.Commands.DeleteWarehouse;

public class DeleteWarehouseCommandHandler : IRequestHandler<DeleteWarehouseCommand, WarehouseDto?>
{
    private readonly IApplicationDbContext _context;

    public DeleteWarehouseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WarehouseDto?> Handle(DeleteWarehouseCommand command, CancellationToken cancellationToken)
    {
        Warehouse? warehouse = await _context.Warehouses
            .Where(x => !x.DeletedOn.HasValue)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);

        if (warehouse == null) return null;

        _context.Warehouses.Remove(warehouse);
        await _context.SaveChangesAsync(cancellationToken);

        return new WarehouseDto
        {
            Id = warehouse.Id,
            WarehouseCode = warehouse.WarehouseCode,
            Name = warehouse.Name,
            Address = warehouse.Address,
            ContactPerson = warehouse.ContactPerson,
            Phone = warehouse.Phone,
            Email = warehouse.Email,
            Capacity = warehouse.Capacity,
            Status = warehouse.Status,
            CreatedOn = warehouse.CreatedOn,
            CreatedBy = warehouse.CreatedBy,
            UpdatedOn = warehouse.UpdatedOn,
            UpdatedBy = warehouse.UpdatedBy,
            DeletedOn = warehouse.DeletedOn,
            DeletedBy = warehouse.DeletedBy
        };
    }
}
