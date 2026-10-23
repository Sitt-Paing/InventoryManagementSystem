using InventoryManagementSystem.Application.Dashboard.DTOs;

namespace InventoryManagementSystem.Application.Common.Interfaces;

public interface IDashboardAiService
{
    Task<DashboardDefinitionDto> GenerateAsync(
        DashboardFactsDto facts,
        string language,
        CancellationToken cancellationToken = default);
}