using InventoryManagementSystem.Application.Master.UnitOfMeasures.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Master.UnitOfMeasures.Queries.GetUnitOfMeasureById;

public record GetUnitOfMeasureByIdQuery(long Id) : IRequest<UnitOfMeasureDto?>;
