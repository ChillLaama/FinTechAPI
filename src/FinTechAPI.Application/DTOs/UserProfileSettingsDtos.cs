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

    public class UserSettingsDto
    {
        public bool EmailNotifications { get; set; }
        public bool PushNotifications { get; set; }
        public bool SmsNotifications { get; set; }
        public bool TransactionAlerts { get; set; }
        public bool SecurityAlerts { get; set; }
        public bool MarketingEmails { get; set; }

        public string Theme { get; set; } = "dark";
        public string Language { get; set; } = "en";

        public bool PublicProfile { get; set; }
        public bool ShowActivity { get; set; } = true;
        public bool DataCollection { get; set; }

        public bool TwoFactorAuth { get; set; }
        public bool Biometric { get; set; } = true;
        public string SessionTimeout { get; set; } = "30";

        public IReadOnlyList<string> LockedFields { get; set; } = [];
    }

    public class UpdateUserSettingsDto
    {
        public bool EmailNotifications { get; set; }
        public bool PushNotifications { get; set; }
        public bool SmsNotifications { get; set; }
        public bool TransactionAlerts { get; set; }
        public bool SecurityAlerts { get; set; }
        public bool MarketingEmails { get; set; }

        public string Theme { get; set; } = "dark";
        public string Language { get; set; } = "en";

        public bool PublicProfile { get; set; }
        public bool ShowActivity { get; set; } = true;
        public bool DataCollection { get; set; }

        public bool TwoFactorAuth { get; set; }
        public bool Biometric { get; set; } = true;
        public string SessionTimeout { get; set; } = "30";
    }

    public class UpdateUserSettingsPolicyDto
    {
        public IReadOnlyList<string> LockedFields { get; set; } = [];
    }
}
