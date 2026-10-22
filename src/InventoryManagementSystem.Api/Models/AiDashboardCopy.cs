namespace InventoryManagementSystem.Api.Models;

public sealed record AiDashboardCopy(
    string Title,
    string Summary,
    Dictionary<string, string> Labels,
    List<AiDashboardProductCopy> Products);

public sealed record AiDashboardProductCopy(
    string Id,
    string DisplayName,
    string UnitLabel,
    string StatusLabel,
    string Explanation);
