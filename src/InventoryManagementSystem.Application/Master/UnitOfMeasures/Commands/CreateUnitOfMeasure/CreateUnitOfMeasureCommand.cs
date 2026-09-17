using InventoryManagementSystem.Application.Master.UnitOfMeasures.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Master.UnitOfMeasures.Commands.CreateUnitOfMeasure;

public record CreateUnitOfMeasureCommand(
    long CategoryId,
    string Code,
    string Name,
    string? Symbol,
    int DecimalPlaces
) : IRequest<UnitOfMeasureDto>;
