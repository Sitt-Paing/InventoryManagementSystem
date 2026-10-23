using FluentValidation;
using InventoryManagementSystem.Application.Dashboard.DTOs;

namespace InventoryManagementSystem.Application.Dashboard.Validators;

public sealed class DashboardDefinitionDtoValidator
    : AbstractValidator<DashboardDefinitionDto>
{
    public DashboardDefinitionDtoValidator()
    {
        RuleFor(definition => definition.Language)
            .Must(language => language is "en" or "mm")
            .WithMessage("Language must be en or mm.");

        RuleFor(definition => definition.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(definition => definition.Summary)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(definition => definition.Widgets)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .NotEmpty()
            .Must(widgets => widgets.Count <= 8)
            .WithMessage("A dashboard may contain at most 8 widgets.")
            .Must(widgets =>
                widgets.All(widget => widget is not null) &&
                widgets.Select(widget => widget.Id)
                    .Distinct(StringComparer.Ordinal)
                    .Count() == widgets.Count)
            .WithMessage("Widgets must be non-null and have unique IDs.");

        RuleForEach(definition => definition.Widgets)
            .NotNull()
            .SetValidator(new DashboardWidgetDtoValidator());
    }
}

public sealed class DashboardWidgetDtoValidator
    : AbstractValidator<DashboardWidgetDto>
{
    private static readonly Dictionary<string, string[]> AllowedTypes =
        new(StringComparer.Ordinal)
        {
            ["activeProductCount"] = ["metric_card"],
            ["openPurchaseOrderCount"] = ["metric_card"],
            ["productStocks"] = ["table", "bar_chart", "pie_chart", "doughnut_chart"],
            ["dailyProductUsage"] = ["table", "bar_chart", "stacked_bar_chart", "line_chart", "area_chart", "pie_chart", "doughnut_chart"]
        };

    private static readonly Dictionary<string, string[]> AllowedFields =
        new(StringComparer.Ordinal)
        {
            ["activeProductCount"] = [],
            ["openPurchaseOrderCount"] = [],
            ["productStocks"] =
            [
                "productName",
                "baseUnit",
                "availableQuantity",
                "reorderLevel"
            ],
            ["dailyProductUsage"] =
            [
                "date",
                "productName",
                "baseUnit",
                "issuedQuantity"
            ]
        };

    public DashboardWidgetDtoValidator()
    {
        RuleFor(widget => widget.Id)
            .NotEmpty()
            .MaximumLength(80)
            .Matches("^[a-z][a-z0-9-]*$")
            .WithMessage("Widget ID must use lowercase letters, numbers and hyphens.");

        RuleFor(widget => widget.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(widget => widget.Description)
            .NotEmpty()
            .MaximumLength(1000);

        RuleFor(widget => widget.DatasetKey)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(key => AllowedTypes.ContainsKey(key))
            .WithMessage("Unsupported dataset.");

        RuleFor(widget => widget.Type)
            .NotEmpty();

        RuleFor(widget => widget.ProductIds)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .Must(ids =>
                ids.All(id => id != Guid.Empty) &&
                ids.Distinct().Count() == ids.Count)
            .WithMessage("Product IDs must be valid and must not be duplicated.");

        RuleFor(widget => widget.Labels)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .Must(labels => labels.Count <= 8)
            .WithMessage("A widget may contain at most 8 field labels.");

        RuleFor(widget => widget).Custom(ValidateWidget);
    }

    private static void ValidateWidget(
        DashboardWidgetDto widget,
        ValidationContext<DashboardWidgetDto> context)
    {
        if (string.IsNullOrWhiteSpace(widget.DatasetKey) ||
            !AllowedTypes.TryGetValue(widget.DatasetKey, out var types))
        {
            return;
        }

        if (!types.Contains(widget.Type, StringComparer.Ordinal))
        {
            context.AddFailure(
                nameof(widget.Type),
                $"Widget type '{widget.Type}' cannot use dataset '{widget.DatasetKey}'.");
        }

        bool isMetric = widget.DatasetKey is
            "activeProductCount" or "openPurchaseOrderCount";

        if (isMetric && widget.ProductIds is { Count: > 0 })
        {
            context.AddFailure(
                nameof(widget.ProductIds),
                "Count metrics do not support product filters.");
        }

        if (widget.Labels is null)
        {
            return;
        }

        var fields = AllowedFields[widget.DatasetKey];
        var usedFields = new HashSet<string>(StringComparer.Ordinal);

        foreach (var label in widget.Labels)
        {
            if (label is null)
            {
                context.AddFailure(nameof(widget.Labels), "Field labels must not be null.");
                continue;
            }

            if (!fields.Contains(label.Field, StringComparer.Ordinal))
            {
                context.AddFailure(
                    nameof(widget.Labels),
                    $"Unsupported label field '{label.Field}'.");
            }

            if (!usedFields.Add(label.Field))
            {
                context.AddFailure(
                    nameof(widget.Labels),
                    $"Duplicate label field '{label.Field}'.");
            }

            if (string.IsNullOrWhiteSpace(label.Text) || label.Text.Length > 200)
            {
                context.AddFailure(
                    nameof(widget.Labels),
                    "Label text is required and must not exceed 200 characters.");
            }
        }

        if (!isMetric && widget.Labels.Count == 0)
        {
            context.AddFailure(
                nameof(widget.Labels),
                "Tables and charts require field labels.");
        }
    }
}
