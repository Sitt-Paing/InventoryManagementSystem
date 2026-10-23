using FluentValidation;
using FluentValidation.Results;
using InventoryManagementSystem.Application.Dashboard.DTOs;

namespace InventoryManagementSystem.Application.Dashboard.Validators;

public sealed class DashboardDefinitionFactsValidator
{
    private readonly IValidator<DashboardDefinitionDto> _definitionValidator;

    public DashboardDefinitionFactsValidator(
        IValidator<DashboardDefinitionDto> definitionValidator)
    {
        _definitionValidator = definitionValidator;
    }

    public async Task<ValidationResult> ValidateAsync(
        DashboardDefinitionDto definition,
        DashboardFactsDto facts,
        string requestedLanguage,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(facts);

        if (requestedLanguage is not ("en" or "mm"))
        {
            throw new ArgumentException("Language must be en or mm.", nameof(requestedLanguage));
        }

        var result = await _definitionValidator.ValidateAsync(definition, cancellationToken);
        if (!result.IsValid)
        {
            return result;
        }

        if (!string.Equals(definition.Language, requestedLanguage, StringComparison.Ordinal))
        {
            result.Errors.Add(new ValidationFailure(
                nameof(definition.Language),
                "The generated dashboard language must match the requested language."));
        }

        for (int index = 0; index < definition.Widgets.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var widget = definition.Widgets[index];
            string path = $"Widgets[{index}]";

            // Scalar count widgets are already checked by the structural validator.
            if (widget.Type == "metric_card")
            {
                continue;
            }

            var requestedIds = widget.ProductIds.ToHashSet();
            bool showAllProducts = requestedIds.Count == 0;

            var datasetIds = widget.DatasetKey == "productStocks"
                ? facts.ProductStocks.Select(product => product.ProductId).ToHashSet()
                : facts.DailyProductUsage.Select(usage => usage.ProductId).ToHashSet();

            if (requestedIds.Any(id => !datasetIds.Contains(id)))
            {
                result.Errors.Add(new ValidationFailure(
                    $"{path}.ProductIds",
                    "Every selected product must exist in the supplied dataset."));
                continue;
            }

            var suppliedFields = widget.Labels.Select(label => label.Field)
                .ToHashSet(StringComparer.Ordinal);
            bool isCircular = widget.Type is "pie_chart" or "doughnut_chart";
            string[] requiredFields = (widget.Type, widget.DatasetKey) switch
            {
                ("table", "productStocks") => ["productName", "baseUnit", "availableQuantity"],
                ("table", "dailyProductUsage") => ["date", "productName", "baseUnit", "issuedQuantity"],
                ("bar_chart", "productStocks") => ["productName", "availableQuantity"],
                ("pie_chart" or "doughnut_chart", "productStocks") => ["productName", "availableQuantity"],
                ("pie_chart" or "doughnut_chart", "dailyProductUsage") => ["productName", "issuedQuantity"],
                _ => ["date", "issuedQuantity"]
            };

            if (requiredFields.Any(field => !suppliedFields.Contains(field)))
            {
                result.Errors.Add(new ValidationFailure(
                    $"{path}.Labels",
                    $"This widget requires labels for: {string.Join(", ", requiredFields)}."));
            }

            // Empty tables are valid empty states, but charts need actual observations.
            if (widget.Type == "table")
            {
                continue;
            }

            string[] units;
            if (widget.DatasetKey == "productStocks")
            {
                units = facts.ProductStocks
                    .Where(product => showAllProducts || requestedIds.Contains(product.ProductId))
                    .Select(product => product.BaseUnit)
                    .ToArray();
            }
            else
            {
                var usageRows = facts.DailyProductUsage
                    .Where(usage => showAllProducts || requestedIds.Contains(usage.ProductId))
                    .ToArray();

                units = usageRows.Select(usage => usage.BaseUnit).ToArray();

                if ((widget.Type is "line_chart" or "area_chart") && usageRows
                    .GroupBy(usage => usage.ProductId)
                    .Any(series => series.Select(usage => usage.Date).Distinct().Count() < 2))
                {
                    result.Errors.Add(new ValidationFailure(
                        $"{path}.Type",
                        "A line or area chart requires at least two recorded dates for each selected product."));
                }
            }

            if (isCircular)
            {
                // Usage shares are period totals grouped by product, never slices per date.
                var values = widget.DatasetKey == "productStocks"
                    ? facts.ProductStocks.Where(product => showAllProducts || requestedIds.Contains(product.ProductId))
                        .Select(product => product.AvailableQuantity).ToArray()
                    : facts.DailyProductUsage.Where(usage => showAllProducts || requestedIds.Contains(usage.ProductId))
                        .GroupBy(usage => usage.ProductId).Select(group => group.Sum(usage => usage.IssuedQuantity)).ToArray();
                if (values.Length is < 2 or > 8 || values.Any(value => value < 0) || values.Sum() <= 0)
                    result.Errors.Add(new ValidationFailure($"{path}.Type",
                        "Pie and doughnut charts require 2 to 8 products, non-negative values and a positive total."));
            }

            if (units.Length == 0)
            {
                result.Errors.Add(new ValidationFailure(
                    $"{path}.DatasetKey",
                    "A chart requires data. Use an empty-state table when no observations exist."));
            }
            else if (units.Any(string.IsNullOrWhiteSpace) ||
                units.Select(unit => unit.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 1)
            {
                result.Errors.Add(new ValidationFailure(
                    $"{path}.ProductIds",
                    "A quantity chart requires one known base unit. Select compatible products or separate widgets."));
            }
        }

        return result;
    }
}
