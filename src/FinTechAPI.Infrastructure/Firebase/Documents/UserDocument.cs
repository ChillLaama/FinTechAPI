using Google.Cloud.Firestore;

namespace FinTechAPI.Infrastructure.Firebase.Documents
{
    [FirestoreData]
    public class UserDocument
    {
        [FirestoreDocumentId]
        public string Id { get; set; } = string.Empty;          // Firebase Auth UID

        [FirestoreProperty("email")]
        public string Email { get; set; } = string.Empty;

        [FirestoreProperty("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [FirestoreProperty("lastName")]
        public string LastName { get; set; } = string.Empty;

        [FirestoreProperty("createdAt")]
        public Timestamp CreatedAt { get; set; }

        [FirestoreProperty("isActive")]
        public bool IsActive { get; set; } = true;
    }
}
