namespace InventoryManagementSystem.Application.Dashboard.DTOs;

public sealed record DashboardFactsDto
{
    public DateTime DataAsOfUtc { get; init; }

    public DateOnly StartDate { get; init; }

    public DateOnly EndDate { get; init; }

    public int ActiveProductCount { get; init; }

    public int OpenPurchaseOrderCount { get; init; }

    public IReadOnlyList<DashboardProductStockDto> ProductStocks
    { get; init; } = [];

    public IReadOnlyList<DashboardDailyUsageDto> DailyProductUsage
    { get; init; } = [];
}

public sealed record DashboardProductStockDto
{
    public Guid ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public string BaseUnit { get; init; } = string.Empty;

    public decimal AvailableQuantity { get; init; }

    public decimal ReorderLevel { get; init; }
}

public sealed record DashboardDailyUsageDto
{
    public DateOnly Date { get; init; }

    public Guid ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public string BaseUnit { get; init; } = string.Empty;

    public decimal IssuedQuantity { get; init; }
}
