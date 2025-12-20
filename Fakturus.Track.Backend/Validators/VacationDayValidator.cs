using Fakturus.Track.Backend.DTOs;
using FluentValidation;

namespace Fakturus.Track.Backend.Validators;

public class CreateVacationDayRequestValidator : AbstractValidator<CreateVacationDayRequest>
{
    public CreateVacationDayRequestValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("Date is required");
    }
}

public class SyncVacationDaysRequestValidator : AbstractValidator<SyncVacationDaysRequest>
{
    public SyncVacationDaysRequestValidator()
    {
        RuleFor(x => x.VacationDays)
            .NotNull()
            .WithMessage("VacationDays list is required");
    }
}