using Devken.CBC.SchoolManagement.Application.DTOs.Academic;
using Devken.CBC.SchoolManagement.Domain.Enums;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Helpers
{
    public class CreateStudentRequestValidator : AbstractValidator<CreateStudentRequest>
    {
        public CreateStudentRequestValidator()
        {
            RuleFor(x => x.DateOfBirth)
                .Must(BeValidAge)
                .WithMessage("Student must be between 2 and 25 years old.");

            RuleFor(x => x.DateOfAdmission)
                .Must((model, admission) =>
                    !admission.HasValue ||
                    (admission.Value >= model.DateOfBirth &&
                     admission.Value <= DateTime.Today))
                .WithMessage("Invalid date of admission.");

            ApplyCbcAgeRules();
        }

        private bool BeValidAge(DateTime dob)
        {
            var age = AgeHelper.CalculateAge(dob);
            return age >= 2 && age <= 25;
        }

        private void ApplyCbcAgeRules()
        {
            RuleFor(x => x)
                .Must(model =>
                {
                    var age = AgeHelper.CalculateAge(model.DateOfBirth);

                    return model.CBCLevel switch
                    {
                        CBCLevel.PP1 => age >= 4,
                        CBCLevel.PP2 => age >= 5,
                        CBCLevel.Grade1 => age >= 6,
                        _ => true
                    };
                })
                .WithMessage("Student does not meet minimum age requirement for selected CBC level.");
        }
    }
}
