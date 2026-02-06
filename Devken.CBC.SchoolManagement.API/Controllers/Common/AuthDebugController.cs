//using Devken.CBC.SchoolManagement.Api.Controllers.Common;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Configuration;
//using System.IdentityModel.Tokens.Jwt;

//namespace Devken.CBC.SchoolManagement.Api.Controllers.Debug
//{
//    /// <summary>
//    /// Debug controller to test and troubleshoot authentication and authorization.
//    /// WARNING: Remove or disable this controller in production!
//    /// </summary>
//    [ApiController]
//    [Route("api/auth-debug")]
//    public class AuthDebugController : BaseApiController
//    {
//        private readonly ILogger<AuthDebugController> _logger;
//        private readonly IConfiguration _configuration;

//        public AuthDebugController(
//            ILogger<AuthDebugController> logger,
//            IConfiguration configuration)
//        {
//            _logger = logger;
//            _configuration = configuration;
//        }

//        /// <summary>
//        /// Test endpoint - No authentication required
//        /// Use this to verify the API is running
//        /// </summary>
//        [HttpGet("public")]
//        [AllowAnonymous]
//        [ProducesResponseType(StatusCodes.Status200OK)]
//        public IActionResult PublicEndpoint()
//        {
//            return Ok(new
//            {
//                Success = true,
//                Message = "✅ This endpoint works without authentication",
//                Timestamp = DateTime.UtcNow,
//                IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
//                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
//            });
//        }

//        /// <summary>
//        /// Test endpoint - Authentication required
//        /// Use this to verify JWT authentication is working
//        /// </summary>
//        [HttpGet("protected")]
//        [Authorize]
//        [ProducesResponseType(StatusCodes.Status200OK)]
//        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
//        public IActionResult ProtectedEndpoint()
//        {
//            try
//            {
//                return SuccessResponse(new
//                {
//                    Message = "✅ Authentication successful! You are authorized.",
//                    UserId = CurrentUserId,
//                    Email = CurrentUserEmail,
//                    UserName = CurrentUserName,
//                    TenantId = CurrentTenantId,
//                    IsSuperAdmin = IsSuperAdmin,
//                    Timestamp = DateTime.UtcNow
//                }, "Protected endpoint accessed successfully");
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error in protected endpoint");
//                return ErrorResponse($"Error: {ex.Message}", 500);
//            }
//        }

//        /// <summary>
//        /// Check what's in the Authorization header
//        /// Use this to verify the token is being sent correctly
//        /// </summary>
//        [HttpGet("check-header")]
//        [AllowAnonymous]
//        [ProducesResponseType(StatusCodes.Status200OK)]
//        public IActionResult CheckAuthHeader()
//        {
//            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
//            var hasBearer = authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ?? false;
//            var tokenPart = hasBearer && authHeader != null ? authHeader.Substring(7) : null;

//            var response = new
//            {
//                HasAuthorizationHeader = authHeader != null,
//                AuthHeaderValue = authHeader != null ? $"{authHeader.Substring(0, Math.Min(20, authHeader.Length))}..." : null,
//                HasBearerPrefix = hasBearer,
//                TokenLength = tokenPart?.Length ?? 0,
//                TokenPreview = tokenPart != null ? $"{tokenPart.Substring(0, Math.Min(30, tokenPart.Length))}..." : null,
//                IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
//                AuthenticationType = User.Identity?.AuthenticationType,
//                ClaimCount = User.Claims.Count(),
//                Diagnosis = GetHeaderDiagnosis(authHeader ?? string.Empty, hasBearer, User.Identity?.IsAuthenticated ?? false)
//            };

//            return Ok(response);
//        }

//        /// <summary>
//        /// Dump all claims from the JWT token
//        /// Use this to verify claims are being added correctly
//        /// </summary>
//        [HttpGet("claims")]
//        [Authorize]
//        [ProducesResponseType(StatusCodes.Status200OK)]
//        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
//        // ✅ Use 'new' keyword to explicitly hide base class method
//        public new IActionResult GetAllClaims()
//        {
//            try
//            {
//                var claims = User.Claims.Select(c => new
//                {
//                    Type = c.Type,
//                    Value = c.Value,
//                    ShortType = GetShortClaimType(c.Type)
//                }).ToList();

//                return SuccessResponse(new
//                {
//                    TotalClaimCount = claims.Count,
//                    Claims = claims,
//                    ParsedUserInfo = new
//                    {
//                        UserId = TryGetClaim("user_id"),
//                        TenantId = TryGetClaim("tenant_id"),
//                        Email = TryGetClaim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")
//                               ?? TryGetClaim("email"),
//                        Name = TryGetClaim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")
//                              ?? TryGetClaim("name"),
//                        IsSuperAdmin = TryGetClaim("is_super_admin"),
//                        PermissionCount = User.FindAll("permissions").Count(),
//                        RoleCount = User.FindAll("http://schemas.microsoft.com/ws/2008/06/identity/claims/role").Count()
//                    },
//                    Permissions = User.FindAll("permissions").Select(c => c.Value).ToList(),
//                    Roles = User.FindAll("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
//                               .Select(c => c.Value).ToList()
//                }, "Claims retrieved successfully");
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error reading claims");
//                return ErrorResponse($"Error reading claims: {ex.Message}", 500);
//            }
//        }

//        /// <summary>
//        /// Test all BaseApiController properties
//        /// Use this to verify BaseApiController is working correctly
//        /// </summary>
//        [HttpGet("base-properties")]
//        [Authorize]
//        [ProducesResponseType(StatusCodes.Status200OK)]
//        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
//        public IActionResult TestBaseProperties()
//        {
//            var result = new Dictionary<string, object>();

//            TryGetProperty("CurrentUserId", () => CurrentUserId, result);
//            TryGetProperty("CurrentUserEmail", () => CurrentUserEmail, result);
//            TryGetProperty("CurrentUserName", () => CurrentUserName, result);
//            TryGetProperty("CurrentTenantId", () => CurrentTenantId?.ToString() ?? "NULL", result);
//            TryGetProperty("IsSuperAdmin", () => IsSuperAdmin, result);
//            TryGetProperty("Permissions", () => CurrentUserPermissions.ToList(), result);
//            TryGetProperty("PermissionCount", () => CurrentUserPermissions.Count(), result);
//            TryGetProperty("Roles", () => CurrentUserRoles.ToList(), result);
//            TryGetProperty("RoleCount", () => CurrentUserRoles.Count(), result);

//            var hasErrors = result.Values.Any(v => v.ToString()?.StartsWith("ERROR:") ?? false);

//            return SuccessResponse(new
//            {
//                HasErrors = hasErrors,
//                Properties = result,
//                Diagnosis = hasErrors
//                    ? "❌ Some properties failed to load. Check the errors above."
//                    : "✅ All BaseApiController properties loaded successfully!"
//            });
//        }

//        /// <summary>
//        /// Validate JWT configuration settings
//        /// Use this to verify appsettings.json is configured correctly
//        /// </summary>
//        [HttpGet("jwt-config")]
//        [AllowAnonymous]
//        [ProducesResponseType(StatusCodes.Status200OK)]
//        public IActionResult GetJwtConfig()
//        {
//            var jwtSettings = _configuration.GetSection("JwtSettings");

//            var issuer = jwtSettings["Issuer"];
//            var audience = jwtSettings["Audience"];
//            var secretKey = jwtSettings["SecretKey"];
//            var accessTokenLifetime = jwtSettings["AccessTokenLifetimeMinutes"];

//            var config = new
//            {
//                HasJwtSection = jwtSettings.Exists(),
//                Issuer = issuer,
//                Audience = audience,
//                HasSecretKey = !string.IsNullOrEmpty(secretKey),
//                SecretKeyLength = secretKey?.Length ?? 0,
//                AccessTokenLifetimeMinutes = accessTokenLifetime,
//                RefreshTokenLifetimeDays = jwtSettings["RefreshTokenLifetimeDays"],
//                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
//                Diagnosis = GetConfigDiagnosis(
//                    jwtSettings.Exists(),
//                    issuer ?? string.Empty,
//                    audience ?? string.Empty,
//                    secretKey ?? string.Empty)
//            };

//            return Ok(config);
//        }

//        /// <summary>
//        /// Test a specific permission
//        /// Use this to verify permission checking is working
//        /// </summary>
//        [HttpGet("test-permission/{permission}")]
//        [Authorize]
//        [ProducesResponseType(StatusCodes.Status200OK)]
//        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
//        public IActionResult TestPermission(string permission)
//        {
//            try
//            {
//                var hasPermission = HasPermission(permission);

//                return SuccessResponse(new
//                {
//                    Permission = permission,
//                    HasPermission = hasPermission,
//                    IsSuperAdmin = IsSuperAdmin,
//                    AllPermissions = CurrentUserPermissions.ToList(),
//                    PermissionCount = CurrentUserPermissions.Count(),
//                    Message = hasPermission
//                        ? $"✅ User has permission: {permission}"
//                        : $"❌ User does NOT have permission: {permission}"
//                });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error testing permission {Permission}", permission);
//                return ErrorResponse($"Error testing permission: {ex.Message}", 500);
//            }
//        }

//        /// <summary>
//        /// Decode and display JWT token information
//        /// Use this to verify token structure and expiration
//        /// </summary>
//        [HttpPost("decode-token")]
//        [AllowAnonymous]
//        [ProducesResponseType(StatusCodes.Status200OK)]
//        public IActionResult DecodeToken([FromBody] TokenRequest request)
//        {
//            try
//            {
//                if (string.IsNullOrWhiteSpace(request.Token))
//                {
//                    return BadRequest(new { Error = "Token is required" });
//                }

//                var handler = new JwtSecurityTokenHandler();
//                var token = handler.ReadJwtToken(request.Token);

//                var now = DateTime.UtcNow;
//                var isExpired = token.ValidTo < now;

//                return Ok(new
//                {
//                    IsValid = handler.CanReadToken(request.Token),
//                    Issuer = token.Issuer,
//                    Audience = token.Audiences.FirstOrDefault(),
//                    IssuedAt = token.ValidFrom,
//                    ExpiresAt = token.ValidTo,
//                    IsExpired = isExpired,
//                    TimeUntilExpiration = isExpired ? "Expired" : (token.ValidTo - now).ToString(),
//                    Claims = token.Claims.Select(c => new
//                    {
//                        Type = GetShortClaimType(c.Type),
//                        Value = c.Value
//                    }).ToList(),
//                    Diagnosis = isExpired
//                        ? "❌ Token is EXPIRED. Please get a new token."
//                        : "✅ Token is valid and not expired."
//                });
//            }
//            catch (Exception ex)
//            {
//                return BadRequest(new
//                {
//                    Error = "Invalid token format",
//                    Message = ex.Message,
//                    Diagnosis = "❌ Token is malformed or invalid. Check the token format."
//                });
//            }
//        }

//        /// <summary>
//        /// Complete authentication diagnostic
//        /// Use this as a comprehensive check of the entire auth system
//        /// </summary>
//        [HttpGet("diagnostic")]
//        [Authorize]
//        [ProducesResponseType(StatusCodes.Status200OK)]
//        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
//        public IActionResult CompleteDiagnostic()
//        {
//            try
//            {
//                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
//                var hasBearer = authHeader?.StartsWith("Bearer ") ?? false;

//                var diagnostic = new
//                {
//                    Step1_HeaderCheck = new
//                    {
//                        Status = authHeader != null ? "✅ PASS" : "❌ FAIL",
//                        HasAuthHeader = authHeader != null,
//                        HasBearerPrefix = hasBearer
//                    },
//                    Step2_Authentication = new
//                    {
//                        Status = User.Identity?.IsAuthenticated == true ? "✅ PASS" : "❌ FAIL",
//                        IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
//                        AuthenticationType = User.Identity?.AuthenticationType
//                    },
//                    Step3_ClaimsExtraction = new
//                    {
//                        Status = User.Claims.Any() ? "✅ PASS" : "❌ FAIL",
//                        ClaimCount = User.Claims.Count(),
//                        HasUserId = User.FindFirst("user_id") != null,
//                        HasEmail = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress") != null,
//                        HasPermissions = User.FindAll("permissions").Any()
//                    },
//                    Step4_BaseController = TestBaseControllerQuick(),
//                    UserInfo = new
//                    {
//                        UserId = CurrentUserId,
//                        Email = CurrentUserEmail,
//                        Name = CurrentUserName,
//                        TenantId = CurrentTenantId,
//                        IsSuperAdmin = IsSuperAdmin
//                    },
//                    Summary = new
//                    {
//                        OverallStatus = "✅ All authentication checks passed!",
//                        Message = "Your authentication system is working correctly."
//                    }
//                };

//                return SuccessResponse(diagnostic, "Diagnostic completed successfully");
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Diagnostic failed");
//                return ErrorResponse($"Diagnostic failed: {ex.Message}", 500);
//            }
//        }

//        // ══════════════════════════════════════════════════════════════
//        // Helper Methods
//        // ══════════════════════════════════════════════════════════════

//        private string TryGetClaim(string claimType)
//        {
//            try
//            {
//                return User.FindFirst(claimType)?.Value ?? "Not Found";
//            }
//            catch
//            {
//                return "Error reading claim";
//            }
//        }

//        private void TryGetProperty<T>(string propertyName, Func<T> getter, Dictionary<string, object> result)
//        {
//            try
//            {
//                result[propertyName] = getter() as object ?? "NULL";
//            }
//            catch (Exception ex)
//            {
//                result[propertyName] = $"ERROR: {ex.Message}";
//            }
//        }

//        private string GetShortClaimType(string claimType)
//        {
//            if (claimType.StartsWith("http://schemas."))
//            {
//                return claimType.Split('/').Last();
//            }
//            return claimType;
//        }

//        private object TestBaseControllerQuick()
//        {
//            try
//            {
//                var _ = CurrentUserId;
//                return new
//                {
//                    Status = "✅ PASS",
//                    Message = "BaseApiController properties work correctly"
//                };
//            }
//            catch (Exception ex)
//            {
//                return new
//                {
//                    Status = "❌ FAIL",
//                    Error = ex.Message
//                };
//            }
//        }

//        private string GetHeaderDiagnosis(string authHeader, bool hasBearer, bool isAuthenticated)
//        {
//            if (string.IsNullOrEmpty(authHeader))
//                return "❌ No Authorization header found. Make sure you're sending the token.";

//            if (!hasBearer)
//                return "❌ Authorization header missing 'Bearer ' prefix.";

//            if (!isAuthenticated)
//                return "❌ Token present but authentication failed. Check token validity and JWT configuration.";

//            return "✅ Authorization header is correctly formatted and authentication succeeded.";
//        }

//        private string GetConfigDiagnosis(bool hasSection, string issuer, string audience, string secretKey)
//        {
//            if (!hasSection)
//                return "❌ JwtSettings section missing in appsettings.json";

//            if (string.IsNullOrEmpty(issuer))
//                return "❌ JWT Issuer is not configured";

//            if (string.IsNullOrEmpty(audience))
//                return "❌ JWT Audience is not configured";

//            if (string.IsNullOrEmpty(secretKey))
//                return "❌ JWT SecretKey is not configured";

//            if (secretKey.Length < 32)
//                return "⚠️ JWT SecretKey is too short (should be at least 32 characters)";

//            return "✅ JWT configuration looks good";
//        }
//    }

//    /// <summary>
//    /// Request model for token decoding endpoint
//    /// </summary>
//    public class TokenRequest
//    {
//        /// <summary>
//        /// JWT token to decode
//        /// </summary>
//        public required string Token { get; set; }
//    }
//}