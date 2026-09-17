using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Master.Warehouses.DTOs;
using InventoryManagementSystem.Domain.Entities;
using MediatR;

namespace InventoryManagementSystem.Application.Master.Warehouses.Commands.CreateWarehouse;

public class CreateWarehouseCommandHandler : IRequestHandler<CreateWarehouseCommand, WarehouseDto>
{
    private readonly IApplicationDbContext _context;
    public CreateWarehouseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WarehouseDto> Handle(CreateWarehouseCommand command, CancellationToken cancellationToken)
    {
        Warehouse entity = new Warehouse
        {
            WarehouseCode = command.WarehouseCode,
            Name = command.Name,
            ContactPerson = command.ContactPerson,
            Address = command.Address,
            Phone = command.Phone,
            Email = command.Email,
            Status = command.Status,
            Capacity = command.Capacity,
        };

        _context.Warehouses.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return new WarehouseDto
        {
            Id = entity.Id,
            WarehouseCode = entity.WarehouseCode,
            Name = entity.Name,
            ContactPerson = entity.ContactPerson,
            Address = entity.Address,
            Phone = entity.Phone,
            Email = entity.Email,
            Capacity = entity.Capacity,
            Status = entity.Status,
            CreatedOn = entity.CreatedOn,
            CreatedBy = entity.CreatedBy
        };
    }
}
