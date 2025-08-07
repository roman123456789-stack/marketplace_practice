using marketplace_practice.Services.dto;

namespace marketplace_practice.Services.interfaces
{
    public interface ITokenService
    {
        public AccessTokenResult GenerateAccessToken(long userId, string email, string role);
        public string GenerateRefreshToken();
    }
}
