using InventoryManagementSystem.Application.Warehouses.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Warehouses.Commands.DeleteWarehouse;

public record class DeleteWarehouseCommand(int Id) : IRequest<WarehouseDto?>;
