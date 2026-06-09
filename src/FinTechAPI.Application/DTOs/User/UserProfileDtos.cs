using System.ComponentModel.DataAnnotations;

namespace FinTechAPI.Application.DTOs
{
    public class UserProfileDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool EmailVerified { get; set; }
        public string Role { get; set; } = string.Empty;
    }

    public class UpdateUserProfileDto
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(200)]
        public string Location { get; set; } = string.Empty;
    }
}

