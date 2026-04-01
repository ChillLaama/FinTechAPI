using Google.Cloud.Firestore;

namespace FinTechAPI.Infrastructure.Firebase.Documents
{
    [FirestoreData]
    public class UserSettingsDocument
    {
        [FirestoreDocumentId]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty("userId")]
        public string UserId { get; set; } = string.Empty;

        [FirestoreProperty("emailNotifications")]
        public bool EmailNotifications { get; set; } = true;

        [FirestoreProperty("pushNotifications")]
        public bool PushNotifications { get; set; }

        [FirestoreProperty("smsNotifications")]
        public bool SmsNotifications { get; set; } = true;

        [FirestoreProperty("transactionAlerts")]
        public bool TransactionAlerts { get; set; } = true;

        [FirestoreProperty("securityAlerts")]
        public bool SecurityAlerts { get; set; } = true;

        [FirestoreProperty("marketingEmails")]
        public bool MarketingEmails { get; set; }

        [FirestoreProperty("theme")]
        public string Theme { get; set; } = "dark";

        [FirestoreProperty("language")]
        public string Language { get; set; } = "en";

        [FirestoreProperty("defaultCurrency")]
        public string DefaultCurrency { get; set; } = "usd";

        [FirestoreProperty("transactionNotifications")]
        public bool TransactionNotifications { get; set; } = true;

        [FirestoreProperty("publicProfile")]
        public bool PublicProfile { get; set; }

        [FirestoreProperty("showActivity")]
        public bool ShowActivity { get; set; } = true;

        [FirestoreProperty("dataCollection")]
        public bool DataCollection { get; set; }

        [FirestoreProperty("twoFactorAuth")]
        public bool TwoFactorAuth { get; set; }

        [FirestoreProperty("biometric")]
        public bool Biometric { get; set; } = true;

        [FirestoreProperty("sessionTimeout")]
        public string SessionTimeout { get; set; } = "30";

        [FirestoreProperty("lockedFields")]
        public List<string> LockedFields { get; set; } = [];

        [FirestoreProperty("createdAt")]
        public Timestamp CreatedAt { get; set; }

        [FirestoreProperty("updatedAt")]
        public Timestamp UpdatedAt { get; set; }
    }
}
