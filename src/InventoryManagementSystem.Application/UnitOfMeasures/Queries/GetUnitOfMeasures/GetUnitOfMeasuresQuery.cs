using InventoryManagementSystem.Application.UnitOfMeasures.DTOs;
using MediatR;
using System.Collections.Generic;

namespace InventoryManagementSystem.Application.UnitOfMeasures.Queries.GetUnitOfMeasures;

public record GetUnitOfMeasuresQuery(long? CategoryId = null) : IRequest<List<UnitOfMeasureDto>>;
