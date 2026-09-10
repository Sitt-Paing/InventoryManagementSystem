using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Warehouses.DTOs;
using InventoryManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Warehouses.Commands.UpdateWarehouse;

public class UpdateWarehouseHandler : IRequestHandler<UpdateWarehouseCommand, WarehouseDto?>
{
    private readonly IApplicationDbContext _context;

    public UpdateWarehouseHandler(IApplicationDbContext context)
    {
        _context = context; 
    }

    public async Task<WarehouseDto?> Handle(UpdateWarehouseCommand command, CancellationToken cancellationToken)
    {
        Warehouse? warehouse = await _context.Warehouses.Where(x => !x.DeletedOn.HasValue).FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (warehouse == null) return null;

        warehouse.WarehouseCode = command.WarehouseCode;
        warehouse.Name = command.Name;
        warehouse.Capacity = command.Capacity;
        warehouse.Address = command.Address;
        warehouse.ContactPerson = command.ContactPerson;
        warehouse.Email = command.Email;
        warehouse.Phone = command.Phone;
        warehouse.Status = command.Status;

        await _context.SaveChangesAsync(cancellationToken);
        return new WarehouseDto
        {
            Id = warehouse.Id,
            WarehouseCode = warehouse.WarehouseCode,
            Name = warehouse.Name,
            Capacity = warehouse.Capacity,
            Address = warehouse.Address,
            ContactPerson = warehouse.ContactPerson,
            Email = warehouse.Email,
            Phone = warehouse.Phone,
            Status = warehouse.Status,
            CreatedOn = warehouse.CreatedOn,
            CreatedBy = warehouse.CreatedBy,
            UpdatedOn = warehouse.UpdatedOn,
            UpdatedBy = warehouse.UpdatedBy
        };
    }
}