using marketplace_practice.Services.dto;
using marketplace_practice.Services.service_models;

namespace marketplace_practice.Services.interfaces
{
    public interface ITokenService
    {
        public AccessTokenResult GenerateAccessToken(long userId, string email, string role, bool is_verified = true);
        public RefreshTokenModel GenerateRefreshToken();
    }
}
