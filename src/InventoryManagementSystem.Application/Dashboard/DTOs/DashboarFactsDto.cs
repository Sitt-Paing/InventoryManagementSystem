namespace InventoryManagementSystem.Application.Dashboard.DTOs;

public sealed record DashboardFactsDto
{
    public DateTime DataAsOfUtc { get; init; }

    public DateOnly SnapshotDate { get; init; }

    public DateOnly StartDate { get; init; }

    public DateOnly EndDate { get; init; }

    public int ActiveProductCount { get; init; }

    public int OpenPurchaseOrderCount { get; init; }

    public IReadOnlyList<DashboardProductStockDto> ProductStocks
    { get; init; } = [];

    public IReadOnlyList<DashboardDailyUsageDto> DailyProductUsage
    { get; init; } = [];

    // Mutually exclusive product counts; quantities in different units are never added.
    public DashboardStockHealthDto StockHealth => new(
        ProductStocks.Count(product => product.AvailableQuantity > product.ReorderLevel && product.AvailableQuantity > 0),
        ProductStocks.Count(product => product.AvailableQuantity > 0 && product.AvailableQuantity <= product.ReorderLevel),
        ProductStocks.Count(product => product.AvailableQuantity <= 0));
}

public sealed record DashboardStockHealthDto(int AboveReorder, int LowStock, int OutOfStock);

public sealed record DashboardProductStockDto
{
    public Guid ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public string BaseUnit { get; init; } = string.Empty;

    public string BaseUnitSymbol { get; init; } = string.Empty;

    public decimal AvailableQuantity { get; init; }

    public decimal ReorderLevel { get; init; }
}

public sealed record DashboardDailyUsageDto
{
    public DateOnly Date { get; init; }

    public Guid ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public string BaseUnit { get; init; } = string.Empty;

    public string BaseUnitSymbol { get; init; } = string.Empty;

    public decimal IssuedQuantity { get; init; }
}
