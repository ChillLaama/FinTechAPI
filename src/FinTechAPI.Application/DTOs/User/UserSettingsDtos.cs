namespace FinTechAPI.Application.DTOs
{
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
        public string DefaultCurrency { get; set; } = "usd";
        public bool TransactionNotifications { get; set; } = true;

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
        public string DefaultCurrency { get; set; } = "usd";
        public bool TransactionNotifications { get; set; } = true;

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
