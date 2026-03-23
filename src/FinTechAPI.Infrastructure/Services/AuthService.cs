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
        private readonly FirestoreProvider _firestore;
        private readonly FirebaseSettings _settings;
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthService(
            FirestoreProvider firestore,
            IOptions<FirebaseSettings> settings,
            IHttpClientFactory httpClientFactory)
        {
            _firestore = firestore;
            _settings = settings.Value;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<(bool Success, string? Error, UserDto? User)> RegisterAsync(RegisterUserDto registerDto)
        {
            try
            {
                // 1. Create user in Firebase Auth
                var userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(new UserRecordArgs
                {
                    Email = registerDto.Email,
                    Password = registerDto.Password,
                    DisplayName = $"{registerDto.FirstName} {registerDto.LastName}"
                });

                // 2. Store profile in Firestore
                var userDoc = new UserDocument
                {
                    Id = userRecord.Uid,
                    Email = registerDto.Email,
                    FirstName = registerDto.FirstName,
                    LastName = registerDto.LastName,
                    Phone = string.Empty,
                    Location = string.Empty,
                    CreatedAt = Timestamp.GetCurrentTimestamp(),
                    UpdatedAt = Timestamp.GetCurrentTimestamp(),
                    IsActive = true
                };
                await _firestore.Users.Document(userRecord.Uid).SetAsync(userDoc);

                // 3. Create default account
                var accountRef = _firestore.Accounts.Document();
                var accountDoc = new AccountDocument
                {
                    Id = accountRef.Id,
                    Name = "Main",
                    AccountType = (int)AccountType.Checking,
                    Balance = 0,
                    Currency = (int)Currency.EUR,
                    UserId = userRecord.Uid,
                    CreatedAt = Timestamp.GetCurrentTimestamp(),
                    UpdatedAt = Timestamp.GetCurrentTimestamp()
                };
                await accountRef.SetAsync(accountDoc);

                var dto = new UserDto
                {
                    Id = userRecord.Uid,
                    Email = registerDto.Email,
                    FirstName = registerDto.FirstName,
                    LastName = registerDto.LastName
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
                var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_settings.WebApiKey}";

                var payload = JsonSerializer.Serialize(new
                {
                    email = loginDto.Email,
                    password = loginDto.Password,
                    returnSecureToken = true
                });

                var response = await client.PostAsync(url,
                    new StringContent(payload, Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                    return new AuthResponseDto { Success = false, ErrorMessage = "Invalid credentials." };

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var idToken = root.GetProperty("idToken").GetString()!;
                var refreshToken = root.GetProperty("refreshToken").GetString()!;
                var expiresIn = int.Parse(root.GetProperty("expiresIn").GetString()!);
                var uid = root.GetProperty("localId").GetString()!;

                // Fetch profile from Firestore
                var userSnap = await _firestore.Users.Document(uid).GetSnapshotAsync();
                UserDto userDto;

                if (userSnap.Exists)
                {
                    var userDoc = userSnap.ConvertTo<UserDocument>();
                    userDto = new UserDto
                    {
                        Id = uid,
                        Email = userDoc.Email,
                        FirstName = userDoc.FirstName,
                        LastName = userDoc.LastName
                    };
                }
                else
                {
                    userDto = new UserDto { Id = uid, Email = loginDto.Email };
                }

                return new AuthResponseDto
                {
                    Success = true,
                    Token = idToken,
                    RefreshToken = refreshToken,
                    Expiration = DateTime.UtcNow.AddSeconds(expiresIn),
                    User = userDto
                };
            }
            catch (Exception ex)
            {
                return new AuthResponseDto { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"https://securetoken.googleapis.com/v1/token?key={_settings.WebApiKey}";

                var payload = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = refreshToken
                });

                var response = await client.PostAsync(url, payload);
                if (!response.IsSuccessStatusCode)
                    return new AuthResponseDto { Success = false, ErrorMessage = "Token refresh failed." };

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var newIdToken = root.GetProperty("id_token").GetString()!;
                var newRefreshToken = root.GetProperty("refresh_token").GetString()!;
                var expiresIn = int.Parse(root.GetProperty("expires_in").GetString()!);
                var uid = root.GetProperty("user_id").GetString()!;

                var userSnap = await _firestore.Users.Document(uid).GetSnapshotAsync();
                UserDto userDto;

                if (userSnap.Exists)
                {
                    var userDoc = userSnap.ConvertTo<UserDocument>();
                    userDto = new UserDto
                    {
                        Id = uid,
                        Email = userDoc.Email,
                        FirstName = userDoc.FirstName,
                        LastName = userDoc.LastName
                    };
                }
                else
                {
                    userDto = new UserDto { Id = uid, Email = "" };
                }

                return new AuthResponseDto
                {
                    Success = true,
                    Token = newIdToken,
                    RefreshToken = newRefreshToken,
                    Expiration = DateTime.UtcNow.AddSeconds(expiresIn),
                    User = userDto
                };
            }
            catch (Exception ex)
            {
                return new AuthResponseDto { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<AuthOperationResultDto> SendPasswordResetEmailAsync(ForgotPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                return new AuthOperationResultDto
                {
                    Success = false,
                    Message = "Email is required."
                };
            }

            return await SendOobCodeAsync("PASSWORD_RESET", email: dto.Email.Trim());
        }

        public async Task<AuthOperationResultDto> ResetPasswordAsync(ResetPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.OobCode))
            {
                return new AuthOperationResultDto
                {
                    Success = false,
                    Message = "Reset code is required."
                };
            }

            if (string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                return new AuthOperationResultDto
                {
                    Success = false,
                    Message = "New password is required."
                };
            }

            var client = _httpClientFactory.CreateClient();
            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:resetPassword?key={_settings.WebApiKey}";
            var payload = JsonSerializer.Serialize(new
            {
                oobCode = dto.OobCode.Trim(),
                newPassword = dto.NewPassword
            });

            var response = await client.PostAsync(
                url,
                new StringContent(payload, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                return await BuildFirebaseErrorResultAsync(response, "Failed to reset password.");
            }

            return new AuthOperationResultDto
            {
                Success = true,
                Message = "Password has been reset successfully."
            };
        }

        public async Task<AuthOperationResultDto> SendEmailVerificationAsync(string idToken)
        {
            if (string.IsNullOrWhiteSpace(idToken))
            {
                return new AuthOperationResultDto
                {
                    Success = false,
                    Message = "Authorization token is required."
                };
            }

            return await SendOobCodeAsync("VERIFY_EMAIL", idToken: idToken.Trim());
        }

        public async Task<AuthOperationResultDto> VerifyEmailAsync(VerifyEmailDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.OobCode))
            {
                return new AuthOperationResultDto
                {
                    Success = false,
                    Message = "Verification code is required."
                };
            }

            var client = _httpClientFactory.CreateClient();
            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:update?key={_settings.WebApiKey}";
            var payload = JsonSerializer.Serialize(new
            {
                oobCode = dto.OobCode.Trim()
            });

            var response = await client.PostAsync(
                url,
                new StringContent(payload, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                return await BuildFirebaseErrorResultAsync(response, "Failed to verify email.");
            }

            return new AuthOperationResultDto
            {
                Success = true,
                Message = "Email has been verified successfully."
            };
        }

        private async Task<AuthOperationResultDto> SendOobCodeAsync(
            string requestType,
            string? email = null,
            string? idToken = null)
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={_settings.WebApiKey}";
            var payload = JsonSerializer.Serialize(new
            {
                requestType,
                email,
                idToken
            });

            var response = await client.PostAsync(
                url,
                new StringContent(payload, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                return await BuildFirebaseErrorResultAsync(response, "Failed to send email.");
            }

            return new AuthOperationResultDto
            {
                Success = true,
                Message = "Request accepted. Check your email inbox."
            };
        }

        private static async Task<AuthOperationResultDto> BuildFirebaseErrorResultAsync(
            HttpResponseMessage response,
            string fallbackMessage)
        {
            try
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var message = doc.RootElement
                    .GetProperty("error")
                    .GetProperty("message")
                    .GetString();

                return new AuthOperationResultDto
                {
                    Success = false,
                    Message = string.IsNullOrWhiteSpace(message) ? fallbackMessage : message
                };
            }
            catch
            {
                return new AuthOperationResultDto
                {
                    Success = false,
                    Message = fallbackMessage
                };
            }
        }
    }
}
