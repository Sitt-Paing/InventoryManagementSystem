namespace InventoryManagementSystem.Application.Dashboard.DTOs;

public sealed record DashboardDefinitionDto
{
    public string Language { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public IReadOnlyList<DashboardWidgetDto> Widgets { get; init; } = [];
}

public sealed record DashboardWidgetDto
{
    public string Id { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public string DatasetKey { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public IReadOnlyList<Guid> ProductIds { get; init; } = [];

    public IReadOnlyList<DashboardFieldLabelDto> Labels { get; init; } = [];
}

public sealed record DashboardFieldLabelDto
{
    public string Field { get; init; } = string.Empty;

    public string Text { get; init; } = string.Empty;
}
