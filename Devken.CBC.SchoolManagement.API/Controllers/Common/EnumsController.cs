using Devken.CBC.SchoolManagement.Api.Controllers.Common;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Devken.CBC.SchoolManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EnumsController : BaseApiController
    {
        #region ===== GENERIC ENUM BUILDER =====

        private static object BuildEnumResponse<TEnum>() where TEnum : Enum
        {
            return Enum.GetValues(typeof(TEnum))
                .Cast<TEnum>()
                .Select(e => new
                {
                    Id = e.ToString().ToLower(),
                    Name = e.ToString(),
                    Value = Convert.ToInt32(e), // numeric value for backend
                    Description = GetEnumDescription(e)
                })
                .ToList();
        }

        private static string GetEnumDescription(Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? value.ToString();
        }

        #endregion

        #region ===== SUBSCRIPTION =====

        [HttpGet("subscription-plans")]
        [AllowAnonymous]
        public IActionResult GetSubscriptionPlans()
            => SuccessResponse(BuildEnumResponse<SubscriptionPlan>());

        [HttpGet("subscription-statuses")]
        [AllowAnonymous]
        public IActionResult GetSubscriptionStatuses()
        {
            var result = Enum.GetValues<SubscriptionStatus>()
                .Select(status => new
                {
                    Id = status.ToString().ToLower(),
                    Name = status.ToString(),
                    Value = (int)status,
                    Description = GetEnumDescription(status),
                    CssClass = GetStatusCssClass(status)
                })
                .ToList();

            return SuccessResponse(result);
        }

        [HttpGet("billing-cycles")]
        [AllowAnonymous]
        public IActionResult GetBillingCycles()
            => SuccessResponse(BuildEnumResponse<BillingCycle>());

        #endregion

        #region ===== STUDENTS =====

        [HttpGet("genders")]
        [AllowAnonymous]
        public IActionResult GetGenders()
            => SuccessResponse(BuildEnumResponse<Gender>());

        [HttpGet("student-statuses")]
        [AllowAnonymous]
        public IActionResult GetStudentStatuses()
            => SuccessResponse(BuildEnumResponse<StudentStatus>());

        [HttpGet("cbc-levels")]
        [AllowAnonymous]
        public IActionResult GetCBCLevels()
            => SuccessResponse(BuildEnumResponse<CBCLevel>());

        #endregion

        #region ===== ACADEMICS =====

        [HttpGet("term-types")]
        [AllowAnonymous]
        public IActionResult GetTermTypes()
            => SuccessResponse(BuildEnumResponse<TermType>());

        [HttpGet("assessment-types")]
        [AllowAnonymous]
        public IActionResult GetAssessmentTypes()
            => SuccessResponse(BuildEnumResponse<AssessmentType>());

        [HttpGet("competency-levels")]
        [AllowAnonymous]
        public IActionResult GetCompetencyLevels()
            => SuccessResponse(BuildEnumResponse<CompetencyLevel>());

        #endregion

        #region ===== TEACHER =====

        [HttpGet("teacher-employment-types")]
        [AllowAnonymous]
        public IActionResult GetTeacherEmploymentTypes()
            => SuccessResponse(BuildEnumResponse<EmploymentType>());

        [HttpGet("teacher-designations")]
        [AllowAnonymous]
        public IActionResult GetTeacherDesignations()
            => SuccessResponse(BuildEnumResponse<Designation>());

        #endregion

        #region ===== PAYMENTS =====

        [HttpGet("payment-statuses")]
        [AllowAnonymous]
        public IActionResult GetPaymentStatuses()
            => SuccessResponse(BuildEnumResponse<PaymentStatus>());

        [HttpGet("mpesa-payment-statuses")]
        [AllowAnonymous]
        public IActionResult GetMpesaPaymentStatuses()
            => SuccessResponse(BuildEnumResponse<PaymentStatus>());

        [HttpGet("mpesa-result-codes")]
        [AllowAnonymous]
        public IActionResult GetMpesaResultCodes()
            => SuccessResponse(BuildEnumResponse<MpesaResultCode>());

        #endregion

        #region ===== ENTITY =====

        [HttpGet("entity-statuses")]
        [AllowAnonymous]
        public IActionResult GetEntityStatuses()
            => SuccessResponse(BuildEnumResponse<EntityStatus>());

        #endregion

        #region ===== CSS HELPER =====

        private static string GetStatusCssClass(SubscriptionStatus status)
        {
            return status switch
            {
                SubscriptionStatus.Active => "bg-success text-white",
                SubscriptionStatus.Suspended => "bg-warning text-dark",
                SubscriptionStatus.Cancelled => "bg-danger text-white",
                SubscriptionStatus.Expired => "bg-danger text-white",
                SubscriptionStatus.GracePeriod => "bg-info text-white",
                SubscriptionStatus.PendingPayment => "bg-info text-white",
                _ => "bg-secondary text-white"
            };
        }

        #endregion
    }
}
