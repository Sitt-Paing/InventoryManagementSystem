using InventoryManagementSystem.Application.WarehouseLocations.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.WarehouseLocations.Commands.CreateWarehouseLocation;

public record class CreateWarehouseLocationCommand(
    int? Id,
    int WarehouseId,
    string LocationCode,
    string? Zone,
    string? Rack,
    string? Bin,
    string? Barcode,
    decimal? Capacity,
    bool Status
) : IRequest<WarehouseLocationDto>;
