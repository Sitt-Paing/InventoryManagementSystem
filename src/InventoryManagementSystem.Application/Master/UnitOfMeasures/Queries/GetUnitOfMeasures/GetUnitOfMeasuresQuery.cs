using InventoryManagementSystem.Application.Master.UnitOfMeasures.DTOs;
using MediatR;
using System.Collections.Generic;

namespace InventoryManagementSystem.Application.Master.UnitOfMeasures.Queries.GetUnitOfMeasures;

public record GetUnitOfMeasuresQuery(long? CategoryId = null) : IRequest<List<UnitOfMeasureDto>>;
