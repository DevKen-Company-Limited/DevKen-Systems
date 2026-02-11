//using System;
//using System.Collections.Generic;

//namespace Devken.CBC.SchoolManagement.Application.Dtos.User
//{
//    public class UserDto
//    {
//        public Guid Id { get; set; }
//        public string Email { get; set; } = string.Empty;
//        public string FirstName { get; set; } = string.Empty;
//        public string LastName { get; set; } = string.Empty;
//        public string? PhoneNumber { get; set; }
//        public string? ProfileImageUrl { get; set; }
//        public Guid SchoolId { get; set; }
//        public string? SchoolName { get; set; }
//        public bool IsActive { get; set; }
//        public bool IsEmailVerified { get; set; }
//        public bool RequirePasswordChange { get; set; }
//        public string? TemporaryPassword { get; set; }
//        public List<string> RoleNames { get; set; } = new();
//        public DateTime CreatedOn { get; set; }
//        public DateTime? UpdatedOn { get; set; }
//    }

//    public class PaginatedUsersResponse
//    {
//        public List<UserDto> Users { get; set; } = new();
//        public int TotalCount { get; set; }
//        public int Page { get; set; }
//        public int PageSize { get; set; }
//        public int TotalPages { get; set; }
//    }
//}