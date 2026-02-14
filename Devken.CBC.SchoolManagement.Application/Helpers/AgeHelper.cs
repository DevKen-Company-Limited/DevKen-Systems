using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devken.CBC.SchoolManagement.Application.Helpers
{
    public static class AgeHelper
    {
        public static int CalculateAge(DateTime dateOfBirth)
        {
            var today = DateTime.Today;
            var age = today.Year - dateOfBirth.Year;

            if (dateOfBirth > today.AddYears(-age))
                age--;

            return age;
        }
    }
}
