using marketplace_practice.Services.dto.Auth;
using marketplace_practice.Services.service_models;
using System.Security.Claims;

namespace marketplace_practice.Services.interfaces
{
    public interface IAuthService
    {
        public Task<Result<AuthTokensDto>> ConfirmEmailAndSignInAsync(string userId, string token);
        public Task<Result<AuthTokensDto>> LoginAsync(string email, string password, bool rememberMe);
        public Task<Result<AuthTokensDto>> RefreshTokenAsync(string token);
        public Task<Result<string>> LogoutAsync(ClaimsPrincipal userPrincipal);
        public Task<Result<string>> RecoveryAsync(string email);
        public Task<Result<string>> ResetPasswordAsync(string email, string token, string newPassword);
        public Task<Result<string>> InitiateEmailChangeAsync(string userId, string newEmail);
        public Task<Result<string>> ConfirmEmailChangeAsync(string userId, string newEmail, string token);
    }
}
