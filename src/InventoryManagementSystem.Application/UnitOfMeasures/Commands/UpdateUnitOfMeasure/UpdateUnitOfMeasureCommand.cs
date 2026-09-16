using InventoryManagementSystem.Application.UnitOfMeasures.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.UnitOfMeasures.Commands.UpdateUnitOfMeasure;

public record UpdateUnitOfMeasureCommand(
    long Id,
    long CategoryId,
    string Code,
    string Name,
    string? Symbol,
    int DecimalPlaces,
    bool IsActive
) : IRequest<UnitOfMeasureDto?>;
