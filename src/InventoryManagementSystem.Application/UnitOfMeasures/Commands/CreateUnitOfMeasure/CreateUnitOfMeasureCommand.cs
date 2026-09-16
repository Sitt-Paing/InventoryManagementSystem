using InventoryManagementSystem.Application.UnitOfMeasures.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.UnitOfMeasures.Commands.CreateUnitOfMeasure;

public record CreateUnitOfMeasureCommand(
    long CategoryId,
    string Code,
    string Name,
    string? Symbol,
    int DecimalPlaces
) : IRequest<UnitOfMeasureDto>;
