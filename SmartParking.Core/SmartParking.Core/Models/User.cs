using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace SmartParking.Core.Models
{
    public class User : BaseModel
    {
        [BsonElement("username")]
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; }

        [BsonElement("email")]
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [BsonElement("passwordHash")]
        [Required(ErrorMessage = "Password hash is required")]
        public string PasswordHash { get; set; }

        [BsonElement("employeeId")]
        [Required(ErrorMessage = "Employee ID is required")]
        public string EmployeeId { get; set; }

        [BsonElement("role")]
        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } // "ADMIN" or "STAFF"

        [BsonElement("firstName")]
        public string FirstName { get; set; }

        [BsonElement("lastName")]
        public string LastName { get; set; }

        [BsonElement("phoneNumber")]
        public string PhoneNumber { get; set; }

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        [BsonElement("lastLogin")]
        public DateTime? LastLogin { get; set; }

        [BsonElement("passwordResetToken")]
        public string PasswordResetToken { get; set; }

        [BsonElement("passwordResetTokenExpiry")]
        public DateTime? PasswordResetTokenExpiry { get; set; }
    }
}
