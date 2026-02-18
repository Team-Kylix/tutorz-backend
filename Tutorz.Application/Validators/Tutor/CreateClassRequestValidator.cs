using FluentValidation;
using Tutorz.Application.DTOs.Tutor;

namespace Tutorz.Application.Validators.Tutor
{
    public class CreateClassRequestValidator : AbstractValidator<CreateClassRequest>
    {
        public CreateClassRequestValidator()
        {
            RuleFor(x => x.InstituteName).NotEmpty().WithMessage("Institute Name is required.");
            RuleFor(x => x.ClassType).NotEmpty().WithMessage("Class Type is required.");
            RuleFor(x => x.Subject).NotEmpty().WithMessage("Subject is required.");
            RuleFor(x => x.StartTime).NotEmpty().WithMessage("Start Time is required.");
            RuleFor(x => x.EndTime).NotEmpty().WithMessage("End Time is required.");
            // Add more specific rules if needed, e.g., regex for time format
        }
    }
}
