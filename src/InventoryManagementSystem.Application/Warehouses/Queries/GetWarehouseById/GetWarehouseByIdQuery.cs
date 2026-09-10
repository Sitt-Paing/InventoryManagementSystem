using InventoryManagementSystem.Application.Warehouses.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Warehouses.Queries.GetWarehouseById;

public record class GetWarehouseByIdQuery(int Id) : IRequest<WarehouseDto?>;
