using marketplace_practice.Controllers.dto;
using marketplace_practice.Services.dto;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace marketplace_practice.Services.interfaces
{
    public interface IAuthService
    {
        public Task<RegisterResultDto> RegisterAsync(RegisterDto dto);
        public Task<IdentityResult> ConfirmEmailAsync(string userId, string token);
        public Task<AuthResultDto> LoginAsync(LoginDto loginDto);
        public Task<AuthResultDto> RefreshTokenAsync(RefreshTokenDto request);
        public Task LogoutAsync(ClaimsPrincipal userPrincipal);
        public Task<RecoveryResultDto> RecoveryAsync(string email);
        public Task<RecoveryResultDto> ResetPasswordAsync(string email, string token, string newPassword);
    }
}
