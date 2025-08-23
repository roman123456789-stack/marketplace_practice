using marketplace_practice.Services.service_models;

namespace marketplace_practice.Services.interfaces
{
    public interface ITokenService
    {
        public Token GenerateAccessToken(long userId, string email, IEnumerable<string> roles, bool is_verified = true);
        public Token GenerateRefreshToken();
    }
}
