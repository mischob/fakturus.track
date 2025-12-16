using FluentValidation;

namespace Fakturus.Track.Backend.Validators;

public class UpdateWorkSessionRequestValidator : AbstractValidator<DTOs.UpdateWorkSessionRequest>
{
    public UpdateWorkSessionRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.StartTime.HasValue || x.StopTime.HasValue || x.Date.HasValue)
            .WithMessage("At least one field must be provided for update");

        RuleFor(x => x)
            .Must(x => !x.StartTime.HasValue || !x.StopTime.HasValue || x.StopTime.Value >= x.StartTime.Value)
            .WithMessage("StopTime must be after StartTime");
    }
}

