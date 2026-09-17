using InventoryManagementSystem.Application.Master.Warehouses.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Master.Warehouses.Commands.DeleteWarehouse;

public record class DeleteWarehouseCommand(int Id) : IRequest<WarehouseDto?>;
