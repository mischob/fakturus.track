using FluentValidation;

namespace Fakturus.Track.Backend.Validators;

public class CreateWorkSessionRequestValidator : AbstractValidator<DTOs.CreateWorkSessionRequest>
{
    public CreateWorkSessionRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Id is required");

        RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("Date is required");

        RuleFor(x => x.StartTime)
            .NotEmpty()
            .WithMessage("StartTime is required");

        RuleFor(x => x)
            .Must(x => !x.StopTime.HasValue || x.StopTime.Value >= x.StartTime)
            .WithMessage("StopTime must be after StartTime");
    }
}

