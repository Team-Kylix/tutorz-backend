using FluentValidation;
using Tutorz.Application.DTOs.Tutor;

namespace Tutorz.Application.Validators.Tutor
{
    public class UpdateTutorProfileDtoValidator : AbstractValidator<UpdateTutorProfileDto>
    {
        public UpdateTutorProfileDtoValidator()
        {
            RuleFor(x => x.FirstName).NotEmpty().WithMessage("First Name is required.");
            RuleFor(x => x.LastName).NotEmpty().WithMessage("Last Name is required.");
            // Add other rules as necessary
        }
    }
}
