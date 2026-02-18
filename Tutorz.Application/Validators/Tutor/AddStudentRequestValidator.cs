using FluentValidation;
using Tutorz.Application.DTOs.Tutor;

namespace Tutorz.Application.Validators.Tutor
{
    public class AddStudentRequestValidator : AbstractValidator<AddStudentRequest>
    {
        public AddStudentRequestValidator()
        {
            RuleFor(x => x.ClassId).NotEmpty().WithMessage("Class Id is required.");
            RuleFor(x => x.StudentRegistrationNumber).NotEmpty().WithMessage("Student Registration Number is required.");
        }
    }
}
