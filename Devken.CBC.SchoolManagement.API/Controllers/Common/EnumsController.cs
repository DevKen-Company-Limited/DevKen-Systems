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
        /// <summary>
        /// Get all subscription plans with metadata
        /// </summary>
        [HttpGet("subscription-plans")]
        [AllowAnonymous]
        public IActionResult GetSubscriptionPlans()
        {
            try
            {
                var plans = Enum.GetValues<SubscriptionPlan>()
                    .Select(plan => new
                    {
                        Id = plan.ToString().ToLower(),
                        Name = plan.ToString(),
                        Value = (int)plan,
                        Description = GetEnumDescription(plan)
                    })
                    .ToList();

                return SuccessResponse(plans, "Subscription plans retrieved successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to retrieve subscription plans: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all billing cycles with metadata
        /// </summary>
        [HttpGet("billing-cycles")]
        [AllowAnonymous]
        public IActionResult GetBillingCycles()
        {
            try
            {
                var cycles = Enum.GetValues<BillingCycle>()
                    .Select(cycle => new
                    {
                        Id = cycle.ToString().ToLower(),
                        Name = cycle.ToString(),
                        Value = (int)cycle,
                        Description = GetEnumDescription(cycle)
                    })
                    .ToList();

                return SuccessResponse(cycles, "Billing cycles retrieved successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to retrieve billing cycles: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all subscription statuses with metadata and CSS classes
        /// </summary>
        [HttpGet("subscription-statuses")]
        [AllowAnonymous]
        public IActionResult GetSubscriptionStatuses()
        {
            try
            {
                var statuses = Enum.GetValues<SubscriptionStatus>()
                    .Select(status => new
                    {
                        Id = status.ToString().ToLower(),
                        Name = status.ToString(),
                        Value = (int)status,
                        Description = GetEnumDescription(status),
                        CssClass = GetStatusCssClass(status)
                    })
                    .ToList();

                return SuccessResponse(statuses, "Subscription statuses retrieved successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to retrieve subscription statuses: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all genders
        /// </summary>
        [HttpGet("genders")]
        [AllowAnonymous]
        public IActionResult GetGenders()
        {
            try
            {
                var genders = Enum.GetValues<Gender>()
                    .Select(gender => new
                    {
                        Id = gender.ToString().ToLower(),
                        Name = gender.ToString(),
                        Value = (int)gender,
                        Description = GetEnumDescription(gender)
                    })
                    .ToList();

                return SuccessResponse(genders);
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to retrieve genders: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all student statuses
        /// </summary>
        [HttpGet("student-statuses")]
        [AllowAnonymous]
        public IActionResult GetStudentStatuses()
        {
            try
            {
                var statuses = Enum.GetValues<StudentStatus>()
                    .Select(status => new
                    {
                        Id = status.ToString().ToLower(),
                        Name = status.ToString(),
                        Value = (int)status,
                        Description = GetEnumDescription(status)
                    })
                    .ToList();

                return SuccessResponse(statuses);
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to retrieve student statuses: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all CBC levels
        /// </summary>
        [HttpGet("cbc-levels")]
        [AllowAnonymous]
        public IActionResult GetCBCLevels()
        {
            try
            {
                var levels = Enum.GetValues<CBCLevel>()
                    .Select(level => new
                    {
                        Id = level.ToString().ToLower(),
                        Name = level.ToString(),
                        Value = (int)level,
                        Description = GetEnumDescription(level)
                    })
                    .ToList();

                return SuccessResponse(levels);
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to retrieve CBC levels: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all term types
        /// </summary>
        [HttpGet("term-types")]
        [AllowAnonymous]
        public IActionResult GetTermTypes()
        {
            try
            {
                var terms = Enum.GetValues<TermType>()
                    .Select(term => new
                    {
                        Id = term.ToString().ToLower(),
                        Name = term.ToString(),
                        Value = (int)term,
                        Description = GetEnumDescription(term)
                    })
                    .ToList();

                return SuccessResponse(terms);
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to retrieve term types: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all assessment types
        /// </summary>
        [HttpGet("assessment-types")]
        [AllowAnonymous]
        public IActionResult GetAssessmentTypes()
        {
            try
            {
                var types = Enum.GetValues<AssessmentType>()
                    .Select(type => new
                    {
                        Id = type.ToString().ToLower(),
                        Name = type.ToString(),
                        Value = (int)type,
                        Description = GetEnumDescription(type)
                    })
                    .ToList();

                return SuccessResponse(types);
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to retrieve assessment types: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all payment statuses
        /// </summary>
        [HttpGet("payment-statuses")]
        [AllowAnonymous]
        public IActionResult GetPaymentStatuses()
        {
            try
            {
                var statuses = Enum.GetValues<PaymentStatus>()
                    .Select(status => new
                    {
                        Id = status.ToString().ToLower(),
                        Name = status.ToString(),
                        Value = (int)status,
                        Description = GetEnumDescription(status)
                    })
                    .ToList();

                return SuccessResponse(statuses);
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Failed to retrieve payment statuses: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper: Get enum description from DescriptionAttribute
        /// </summary>
        private static string GetEnumDescription(Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? value.ToString();
        }

        /// <summary>
        /// Helper: Get CSS class based on subscription status
        /// </summary>
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
    }
}