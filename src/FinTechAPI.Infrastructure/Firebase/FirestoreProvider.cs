using Google.Cloud.Firestore;

namespace FinTechAPI.Infrastructure.Firebase
{
    public class FirestoreProvider
    {
        private readonly FirestoreDb _db;

        public FirestoreProvider(FirestoreDb db)
        {
            _db = db;
        }

        public CollectionReference Accounts     => _db.Collection("accounts");
        public CollectionReference Transactions => _db.Collection("transactions");
        public CollectionReference Users        => _db.Collection("users");
    }
}
