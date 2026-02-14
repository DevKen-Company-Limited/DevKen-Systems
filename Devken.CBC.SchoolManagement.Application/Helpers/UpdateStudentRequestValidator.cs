using Devken.CBC.SchoolManagement.Application.DTOs.Academic;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Helpers
{
    public class UpdateStudentRequestValidator : AbstractValidator<UpdateStudentRequest>
    {
        public UpdateStudentRequestValidator()
        {
            RuleFor(x => x.DateOfBirth)
                .Must(dob =>
                {
                    var age = AgeHelper.CalculateAge(dob);
                    return age >= 2 && age <= 25;
                })
                .WithMessage("Student must be between 2 and 25 years old.");

            RuleFor(x => x.DateOfAdmission)
                .Must((model, admission) =>
                    !admission.HasValue ||
                    (admission.Value >= model.DateOfBirth &&
                     admission.Value <= DateTime.Today))
                .WithMessage("Invalid date of admission.");
        }
    }
}
