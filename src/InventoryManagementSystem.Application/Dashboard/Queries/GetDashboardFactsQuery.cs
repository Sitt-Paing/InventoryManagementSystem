using InventoryManagementSystem.Application.Dashboard.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Dashboard.Queries.GetDashboardFacts;

public sealed record GetDashboardFactsQuery(
    DateOnly StartDate,
    DateOnly EndDate
) : IRequest<DashboardFactsDto>;