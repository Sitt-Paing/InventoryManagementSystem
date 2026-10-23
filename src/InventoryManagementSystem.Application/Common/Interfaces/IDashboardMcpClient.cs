using System.Text.Json;
using InventoryManagementSystem.Application.Dashboard.DTOs;

namespace InventoryManagementSystem.Application.Common.Interfaces;

public interface IDashboardMcpClient
{
    Task<IReadOnlyList<DashboardToolDefinitionDto>> GetToolsAsync(CancellationToken cancellationToken);

    Task<DashboardFactsDto> GetFactsAsync(
        DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken);
}

public sealed record DashboardToolDefinitionDto(string Name, string Description, JsonElement InputSchema);
