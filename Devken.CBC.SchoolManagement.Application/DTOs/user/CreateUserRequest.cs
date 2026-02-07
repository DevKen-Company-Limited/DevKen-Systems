//using System;
//using System.Collections.Generic;

//namespace Devken.CBC.SchoolManagement.Application.Dtos.User
//{
//    public class CreateUserRequest
//    {
//        public Guid? SchoolId { get; set; } // For SuperAdmin to specify
//        public string Email { get; set; } = string.Empty;
//        public string FirstName { get; set; } = string.Empty;
//        public string LastName { get; set; } = string.Empty;
//        public string? PhoneNumber { get; set; }
//        public string? TemporaryPassword { get; set; }
//        public bool RequirePasswordChange { get; set; } = true;
//        public List<string> RoleIds { get; set; } = new();
//    }

//    public class UpdateUserRequest
//    {
//        public string? Email { get; set; }
//        public string? FirstName { get; set; }
//        public string? LastName { get; set; }
//        public string? PhoneNumber { get; set; }
//        public string? ProfileImageUrl { get; set; }
//        public bool? IsActive { get; set; }
//    }
//}