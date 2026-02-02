using Devken.CBC.SchoolManagement.API.Authorization;
using Devken.CBC.SchoolManagement.Application.Dtos;
using Devken.CBC.SchoolManagement.Application.RepositoryManagers.Interfaces.Common;
using Devken.CBC.SchoolManagement.Domain.Entities.Identity;
using Devken.CBC.SchoolManagement.Domain.Enums;
using Devken.CBC.SchoolManagement.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Devken.CBC.SchoolManagement.API.Controllers.Identity
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UserManagementController : ControllerBase
    {
        private readonly IRepositoryManager _repo;
        private readonly ILogger<UserManagementController> _logger;

        public UserManagementController(
            IRepositoryManager repo,
            ILogger<UserManagementController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        // ── GET USERS ────────────────────────────────────────
        [HttpGet]
        [RequirePermission("User.Read")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var tenantClaim = HttpContext.User.FindFirst(CustomClaimTypes.TenantId);
                if (tenantClaim == null)
                    return Unauthorized("Tenant information missing.");

                var tenantId = Guid.Parse(tenantClaim.Value);

                var users = await _repo.User
                    .FindByCondition(u => u.TenantId == tenantId, trackChanges: false)
                    .Select(u => new
                    {
                        u.Id,
                        u.Email,
                        u.FullName,
                        u.IsActive,
                        u.RequirePasswordChange
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching users");
                return StatusCode(500, "An error occurred while retrieving users.");
            }
        }

        // ── CREATE USER ──────────────────────────────────────
        [HttpPost]
        [RequirePermission("User.Write")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var tenantClaim = HttpContext.User.FindFirst(CustomClaimTypes.TenantId);
                if (tenantClaim == null)
                    return Unauthorized("Tenant information missing.");

                var tenantId = Guid.Parse(tenantClaim.Value);

                if (await _repo.User.EmailExistsInTenantAsync(dto.Email, tenantId))
                    return Conflict(new { Message = "Email already exists in this school." });

                var user = new User
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Email = dto.Email,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    PasswordHash = PasswordHelper.HashPassword(dto.TemporaryPassword),
                    IsActive = true,
                    RequirePasswordChange = true
                };

                _repo.User.Create(user);

                if (dto.RoleId.HasValue)
                {
                    _repo.UserRole.Create(new UserRole
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        UserId = user.Id,
                        RoleId = dto.RoleId.Value
                    });
                }

                await _repo.SaveAsync();

                return CreatedAtAction(nameof(GetUsers), new { id = user.Id }, new
                {
                    user.Id,
                    user.Email,
                    user.FullName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, "An error occurred while creating the user.");
            }
        }

        // ── DELETE USER ──────────────────────────────────────
        [HttpDelete("{userId:guid}")]
        [RequirePermission("User.Delete")]
        public async Task<IActionResult> DeleteUser(Guid userId)
        {
            try
            {
                var tenantClaim = HttpContext.User.FindFirst(CustomClaimTypes.TenantId);
                if (tenantClaim == null)
                    return Unauthorized("Tenant information missing.");

                var tenantId = Guid.Parse(tenantClaim.Value);

                var user = await _repo.User.GetByIdAsync(userId);
                if (user == null || user.TenantId != tenantId)
                    return NotFound();

                user.Status = EntityStatus.Deleted;
                _repo.User.Update(user);

                _repo.RefreshToken.RevokeAllByUserId(userId);

                await _repo.SaveAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                return StatusCode(500, "An error occurred while deleting the user.");
            }
        }
    }
}
