using FluentValidation;
using Tutorz.Application.DTOs.Tutor;

namespace Tutorz.Application.Validators.Tutor
{
    public class ProcessRequestDtoValidator : AbstractValidator<ProcessRequestDto>
    {
        public ProcessRequestDtoValidator()
        {
            RuleFor(x => x.EnrollmentIds).NotEmpty().WithMessage("Enrollment Ids are required.");
            RuleFor(x => x.Action).NotEmpty().WithMessage("Action is required.")
                .Must(x => x == "Accept" || x == "Reject").WithMessage("Action must be either 'Accept' or 'Reject'.");
        }
    }
}
