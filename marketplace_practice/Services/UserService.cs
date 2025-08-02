using marketplace_practice.Controllers.dto;
using marketplace_practice.Services.dto;
using marketplace_practice.Services.interfaces;

namespace marketplace_practice.Services
{
    public class UserService : IUserService
    {
        private readonly TokenService _tokenService;

        public UserService(TokenService tokenService)
        {
            _tokenService = tokenService;
        }

        public string GetUserById()
        {
            return "Dev version";
        }

        public CreateUserResultDto CreateUser(CreateUserDto dto)
        {
            AccessTokenResult accessTokenData = _tokenService.GenerateAccessToken(123, dto.Email, dto.Role);
            string refresh_token = _tokenService.GenerateRefreshToken();
            return new CreateUserResultDto { 
                AccessToken = accessTokenData.AccessToken,
                ExpiresAt = accessTokenData.ExpiresAt,
                RefreshToken = refresh_token,
            };
        }

        public string UpdateUser()
        {
            return "Dev version";
        }

        public string DeleteUser()
        {
            return "Dev version";
        }
    }
}
