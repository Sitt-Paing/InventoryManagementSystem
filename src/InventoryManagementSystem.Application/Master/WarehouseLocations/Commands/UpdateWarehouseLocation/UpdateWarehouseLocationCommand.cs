using InventoryManagementSystem.Application.Master.WarehouseLocations.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Master.WarehouseLocations.Commands.UpdateWarehouseLocation;

public record class UpdateWarehouseLocationCommand(
    int Id,
    int WarehouseId,
    string LocationCode,
    string? Zone,
    string? Rack,
    string? Bin,
    string? Barcode,
    decimal? Capacity,
    bool Status
) : IRequest<WarehouseLocationDto?>;
