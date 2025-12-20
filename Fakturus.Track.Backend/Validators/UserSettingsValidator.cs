using Fakturus.Track.Backend.DTOs;
using FluentValidation;

namespace Fakturus.Track.Backend.Validators;

public class UpdateUserSettingsRequestValidator : AbstractValidator<UpdateUserSettingsRequest>
{
    public UpdateUserSettingsRequestValidator()
    {
        RuleFor(x => x.VacationDaysPerYear)
            .GreaterThan(0)
            .LessThanOrEqualTo(365)
            .WithMessage("Vacation days per year must be between 1 and 365");

        RuleFor(x => x.WorkHoursPerWeek)
            .GreaterThan(0)
            .LessThanOrEqualTo(168)
            .WithMessage("Work hours per week must be between 0 and 168");
    }
}

