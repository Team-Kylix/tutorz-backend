using FluentValidation;
using Tutorz.Application.DTOs.Tutor;

namespace Tutorz.Application.Validators.Tutor
{
    public class TutorProfileDtoValidator : AbstractValidator<TutorProfileDto>
    {
        public TutorProfileDtoValidator()
        {
             // TutorProfileDto seems to be used for response mainly, or updates?
             // If used for update (PUT), we should validate. 
             // The controller uses it in `UpdateProfile([FromBody] TutorProfileDto request)`
             
             RuleFor(x => x.FirstName).NotEmpty().WithMessage("First Name is required.");
             RuleFor(x => x.LastName).NotEmpty().WithMessage("Last Name is required.");
             // Email and Phone might be read-only from User table in some contexts, but if updatable here:
             RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
             
        }
    }
}
