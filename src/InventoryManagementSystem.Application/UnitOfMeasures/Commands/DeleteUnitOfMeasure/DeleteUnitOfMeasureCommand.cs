using InventoryManagementSystem.Application.UnitOfMeasures.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.UnitOfMeasures.Commands.DeleteUnitOfMeasure;

public record DeleteUnitOfMeasureCommand(long Id) : IRequest<UnitOfMeasureDto?>;
