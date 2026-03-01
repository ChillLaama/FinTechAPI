using System.Text;
using System.Text.Json;
using FirebaseAdmin.Auth;
using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Interfaces;
using FinTechAPI.Domain.Models;
using FinTechAPI.Infrastructure.Firebase;
using FinTechAPI.Infrastructure.Firebase.Documents;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Options;

namespace FinTechAPI.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly FirestoreProvider   _firestore;
        private readonly FirebaseSettings    _settings;
        private readonly IHttpClientFactory  _httpClientFactory;

        public AuthService(
            FirestoreProvider firestore,
            IOptions<FirebaseSettings> settings,
            IHttpClientFactory httpClientFactory)
        {
            _firestore         = firestore;
            _settings          = settings.Value;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<(bool Success, string? Error, UserDto? User)> RegisterAsync(RegisterUserDto registerDto)
        {
            try
            {
                // 1. Create user in Firebase Auth
                var userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(new UserRecordArgs
                {
                    Email       = registerDto.Email,
                    Password    = registerDto.Password,
                    DisplayName = $"{registerDto.FirstName} {registerDto.LastName}"
                });

                // 2. Store profile in Firestore
                var userDoc = new UserDocument
                {
                    Id        = userRecord.Uid,
                    Email     = registerDto.Email,
                    FirstName = registerDto.FirstName,
                    LastName  = registerDto.LastName,
                    CreatedAt = Timestamp.GetCurrentTimestamp(),
                    IsActive  = true
                };
                await _firestore.Users.Document(userRecord.Uid).SetAsync(userDoc);

                // 3. Create default account
                var accountRef = _firestore.Accounts.Document();
                var accountDoc = new AccountDocument
                {
                    Id          = accountRef.Id,
                    Name        = "Main",
                    AccountType = (int)AccountType.Checking,
                    Balance     = 0,
                    Currency    = (int)Currency.USD,
                    UserId      = userRecord.Uid,
                    CreatedAt   = Timestamp.GetCurrentTimestamp(),
                    UpdatedAt   = Timestamp.GetCurrentTimestamp()
                };
                await accountRef.SetAsync(accountDoc);

                var dto = new UserDto
                {
                    Id        = userRecord.Uid,
                    Email     = registerDto.Email,
                    FirstName = registerDto.FirstName,
                    LastName  = registerDto.LastName
                };

                return (true, null, dto);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            try
            {
                // Call Firebase Auth REST API to sign in with email/password
                var client = _httpClientFactory.CreateClient();
                var url    = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_settings.WebApiKey}";

                var payload = JsonSerializer.Serialize(new
                {
                    email             = loginDto.Email,
                    password          = loginDto.Password,
                    returnSecureToken = true
                });

                var response = await client.PostAsync(url,
                    new StringContent(payload, Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                    return new AuthResponseDto { Success = false, ErrorMessage = "Invalid credentials." };

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var idToken     = root.GetProperty("idToken").GetString()!;
                var refreshToken = root.GetProperty("refreshToken").GetString()!;
                var expiresIn   = int.Parse(root.GetProperty("expiresIn").GetString()!);
                var uid         = root.GetProperty("localId").GetString()!;

                // Fetch profile from Firestore
                var userSnap = await _firestore.Users.Document(uid).GetSnapshotAsync();
                UserDto userDto;

                if (userSnap.Exists)
                {
                    var userDoc   = userSnap.ConvertTo<UserDocument>();
                    userDto = new UserDto
                    {
                        Id        = uid,
                        Email     = userDoc.Email,
                        FirstName = userDoc.FirstName,
                        LastName  = userDoc.LastName
                    };
                }
                else
                {
                    userDto = new UserDto { Id = uid, Email = loginDto.Email };
                }

                return new AuthResponseDto
                {
                    Success      = true,
                    Token        = idToken,
                    RefreshToken = refreshToken,
                    Expiration   = DateTime.UtcNow.AddSeconds(expiresIn),
                    User         = userDto
                };
            }
            catch (Exception ex)
            {
                return new AuthResponseDto { Success = false, ErrorMessage = ex.Message };
            }
        }
    }
}
