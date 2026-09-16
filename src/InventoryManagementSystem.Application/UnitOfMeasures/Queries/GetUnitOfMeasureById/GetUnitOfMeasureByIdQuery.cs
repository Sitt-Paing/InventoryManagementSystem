using InventoryManagementSystem.Application.UnitOfMeasures.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.UnitOfMeasures.Queries.GetUnitOfMeasureById;

public record GetUnitOfMeasureByIdQuery(long Id) : IRequest<UnitOfMeasureDto?>;
