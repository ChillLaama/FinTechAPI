using FirebaseAdmin.Auth;
using FinTechAPI.Application.DTOs;
using FinTechAPI.Infrastructure.Firebase;
using FinTechAPI.Infrastructure.Firebase.Documents;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinTechAPI.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private static readonly HashSet<string> AllowedPolicyFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "emailNotifications",
            "pushNotifications",
            "smsNotifications",
            "transactionAlerts",
            "securityAlerts",
            "marketingEmails",
            "theme",
            "language",
            "publicProfile",
            "showActivity",
            "dataCollection",
            "twoFactorAuth",
            "biometric",
            "sessionTimeout"
        };

        private readonly FirestoreProvider _firestore;

        public UsersController(FirestoreProvider firestore)
        {
            _firestore = firestore;
        }

        private string GetCurrentUserId() =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        private string GetCurrentUserRole() =>
            User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var pagedEnumerable = FirebaseAuth.DefaultInstance.ListUsersAsync(null);
            var users = new List<object>();

            await foreach (var user in pagedEnumerable)
            {
                users.Add(new
                {
                    user.Uid,
                    user.Email,
                    user.DisplayName,
                    user.Disabled
                });
            }

            return Ok(users);
        }

        [HttpGet("{uid}")]
        public async Task<IActionResult> GetUser(string uid)
        {
            try
            {
                var user = await FirebaseAuth.DefaultInstance.GetUserAsync(uid);
                return Ok(new
                {
                    user.Uid,
                    user.Email,
                    user.DisplayName,
                    user.Disabled
                });
            }
            catch (FirebaseAuthException)
            {
                return NotFound(new { message = $"User {uid} not found." });
            }
        }

        [HttpDelete("{uid}")]
        public async Task<IActionResult> DeleteUser(string uid)
        {
            try
            {
                await FirebaseAuth.DefaultInstance.DeleteUserAsync(uid);
                return NoContent();
            }
            catch (FirebaseAuthException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{uid}/disable")]
        public async Task<IActionResult> DisableUser(string uid)
        {
            try
            {
                await FirebaseAuth.DefaultInstance.UpdateUserAsync(new UserRecordArgs
                {
                    Uid      = uid,
                    Disabled = true
                });
                return NoContent();
            }
            catch (FirebaseAuthException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("me/profile")]
        public async Task<ActionResult<UserProfileDto>> GetMyProfile()
        {
            var uid = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(uid))
                return Unauthorized();

            var profile = await LoadOrCreateProfileAsync(uid);
            var firebaseUser = await FirebaseAuth.DefaultInstance.GetUserAsync(uid);

            return Ok(new UserProfileDto
            {
                Id = uid,
                Email = profile.Email,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                Phone = profile.Phone,
                Location = profile.Location,
                CreatedAt = profile.CreatedAt.ToDateTime(),
                EmailVerified = firebaseUser.EmailVerified,
                Role = GetCurrentUserRole()
            });
        }

        [HttpPatch("me/profile")]
        public async Task<ActionResult<UserProfileDto>> UpdateMyProfile([FromBody] UpdateUserProfileDto dto)
        {
            var uid = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(uid))
                return Unauthorized();

            var profile = await LoadOrCreateProfileAsync(uid);
            profile.FirstName = dto.FirstName.Trim();
            profile.LastName = dto.LastName.Trim();
            profile.Phone = dto.Phone.Trim();
            profile.Location = dto.Location.Trim();
            profile.UpdatedAt = Timestamp.GetCurrentTimestamp();

            await _firestore.Users.Document(uid).SetAsync(profile, SetOptions.Overwrite);

            await FirebaseAuth.DefaultInstance.UpdateUserAsync(new UserRecordArgs
            {
                Uid = uid,
                DisplayName = $"{profile.FirstName} {profile.LastName}".Trim()
            });

            var firebaseUser = await FirebaseAuth.DefaultInstance.GetUserAsync(uid);
            return Ok(new UserProfileDto
            {
                Id = uid,
                Email = profile.Email,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                Phone = profile.Phone,
                Location = profile.Location,
                CreatedAt = profile.CreatedAt.ToDateTime(),
                EmailVerified = firebaseUser.EmailVerified,
                Role = GetCurrentUserRole()
            });
        }

        [HttpGet("me/settings")]
        public async Task<ActionResult<UserSettingsDto>> GetMySettings()
        {
            var uid = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(uid))
                return Unauthorized();

            var settings = await LoadOrCreateSettingsAsync(uid);
            return Ok(ToSettingsDto(settings));
        }

        [HttpPatch("me/settings")]
        public async Task<ActionResult<UserSettingsDto>> UpdateMySettings([FromBody] UpdateUserSettingsDto dto)
        {
            var uid = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(uid))
                return Unauthorized();

            var settings = await LoadOrCreateSettingsAsync(uid);
            ApplySettingsUpdate(settings, dto, settings.LockedFields);
            settings.UpdatedAt = Timestamp.GetCurrentTimestamp();

            await _firestore.UserSettings.Document(uid).SetAsync(settings, SetOptions.Overwrite);
            return Ok(ToSettingsDto(settings));
        }

        [Authorize(Roles = "admin")]
        [HttpPatch("{uid}/settings-policy")]
        public async Task<ActionResult<UserSettingsDto>> UpdateUserSettingsPolicy(
            string uid,
            [FromBody] UpdateUserSettingsPolicyDto dto)
        {
            if (string.IsNullOrWhiteSpace(uid))
                return BadRequest(new { message = "User id is required." });

            var invalidField = dto.LockedFields
                .FirstOrDefault(field => !AllowedPolicyFields.Contains(field));

            if (invalidField is not null)
            {
                return BadRequest(new { message = $"Unsupported policy field: {invalidField}" });
            }

            var settings = await LoadOrCreateSettingsAsync(uid);
            settings.LockedFields = dto.LockedFields
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            settings.UpdatedAt = Timestamp.GetCurrentTimestamp();

            await _firestore.UserSettings.Document(uid).SetAsync(settings, SetOptions.Overwrite);
            return Ok(ToSettingsDto(settings));
        }

        private async Task<UserDocument> LoadOrCreateProfileAsync(string uid)
        {
            var userDocRef = _firestore.Users.Document(uid);
            var userDocSnap = await userDocRef.GetSnapshotAsync();
            if (userDocSnap.Exists)
                return userDocSnap.ConvertTo<UserDocument>();

            var firebaseUser = await FirebaseAuth.DefaultInstance.GetUserAsync(uid);
            var (firstName, lastName) = SplitDisplayName(firebaseUser.DisplayName);

            var profile = new UserDocument
            {
                Id = uid,
                Email = firebaseUser.Email ?? string.Empty,
                FirstName = firstName,
                LastName = lastName,
                Phone = firebaseUser.PhoneNumber ?? string.Empty,
                Location = string.Empty,
                CreatedAt = Timestamp.GetCurrentTimestamp(),
                UpdatedAt = Timestamp.GetCurrentTimestamp(),
                IsActive = !firebaseUser.Disabled
            };

            await userDocRef.SetAsync(profile);
            return profile;
        }

        private async Task<UserSettingsDocument> LoadOrCreateSettingsAsync(string uid)
        {
            var settingsRef = _firestore.UserSettings.Document(uid);
            var settingsSnap = await settingsRef.GetSnapshotAsync();
            if (settingsSnap.Exists)
            {
                return settingsSnap.ConvertTo<UserSettingsDocument>();
            }

            var settings = new UserSettingsDocument
            {
                Id = uid,
                UserId = uid,
                CreatedAt = Timestamp.GetCurrentTimestamp(),
                UpdatedAt = Timestamp.GetCurrentTimestamp()
            };

            await settingsRef.SetAsync(settings);
            return settings;
        }

        private static UserSettingsDto ToSettingsDto(UserSettingsDocument settings) => new()
        {
            EmailNotifications = settings.EmailNotifications,
            PushNotifications = settings.PushNotifications,
            SmsNotifications = settings.SmsNotifications,
            TransactionAlerts = settings.TransactionAlerts,
            SecurityAlerts = settings.SecurityAlerts,
            MarketingEmails = settings.MarketingEmails,
            Theme = settings.Theme,
            Language = settings.Language,
            PublicProfile = settings.PublicProfile,
            ShowActivity = settings.ShowActivity,
            DataCollection = settings.DataCollection,
            TwoFactorAuth = settings.TwoFactorAuth,
            Biometric = settings.Biometric,
            SessionTimeout = settings.SessionTimeout,
            LockedFields = settings.LockedFields
        };

        private static void ApplySettingsUpdate(
            UserSettingsDocument target,
            UpdateUserSettingsDto dto,
            IReadOnlyCollection<string> lockedFields)
        {
            var lockSet = new HashSet<string>(lockedFields, StringComparer.OrdinalIgnoreCase);

            if (!lockSet.Contains("emailNotifications")) target.EmailNotifications = dto.EmailNotifications;
            if (!lockSet.Contains("pushNotifications")) target.PushNotifications = dto.PushNotifications;
            if (!lockSet.Contains("smsNotifications")) target.SmsNotifications = dto.SmsNotifications;
            if (!lockSet.Contains("transactionAlerts")) target.TransactionAlerts = dto.TransactionAlerts;
            if (!lockSet.Contains("securityAlerts")) target.SecurityAlerts = dto.SecurityAlerts;
            if (!lockSet.Contains("marketingEmails")) target.MarketingEmails = dto.MarketingEmails;
            if (!lockSet.Contains("theme")) target.Theme = dto.Theme;
            if (!lockSet.Contains("language")) target.Language = dto.Language;
            if (!lockSet.Contains("publicProfile")) target.PublicProfile = dto.PublicProfile;
            if (!lockSet.Contains("showActivity")) target.ShowActivity = dto.ShowActivity;
            if (!lockSet.Contains("dataCollection")) target.DataCollection = dto.DataCollection;
            if (!lockSet.Contains("twoFactorAuth")) target.TwoFactorAuth = dto.TwoFactorAuth;
            if (!lockSet.Contains("biometric")) target.Biometric = dto.Biometric;
            if (!lockSet.Contains("sessionTimeout")) target.SessionTimeout = dto.SessionTimeout;
        }

        private static (string FirstName, string LastName) SplitDisplayName(string? displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return (string.Empty, string.Empty);

            var parts = displayName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return (parts[0], string.Empty);

            return (parts[0], parts[1]);
        }
    }
}
