using InventoryManagementSystem.Application.Master.UnitOfMeasures.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Master.UnitOfMeasures.Commands.DeleteUnitOfMeasure;

public record DeleteUnitOfMeasureCommand(long Id) : IRequest<UnitOfMeasureDto?>;
