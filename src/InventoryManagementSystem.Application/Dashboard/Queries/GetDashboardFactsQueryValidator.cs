using FluentValidation;

namespace InventoryManagementSystem.Application.Dashboard.Queries.GetDashboardFacts;

public sealed class GetDashboardFactsQueryValidator
    : AbstractValidator<GetDashboardFactsQuery>
{
    public GetDashboardFactsQueryValidator()
    {
        RuleFor(query => query.StartDate)
            .NotEmpty()
            .WithMessage("Start date is required.");

        RuleFor(query => query.EndDate)
            .NotEmpty()
            .WithMessage("End date is required.")
            .GreaterThanOrEqualTo(query => query.StartDate)
            .WithMessage(
                "End date must not be before start date.")
            .LessThan(DateOnly.MaxValue)
            .WithMessage("End date is outside the supported range.");

        RuleFor(query => query)
            .Must(query =>
                query.EndDate.DayNumber - query.StartDate.DayNumber < 90)
            .WithMessage(
                "Select a period of no more than 90 days.");
    }
}