using marketplace_practice.Controllers.dto;
using marketplace_practice.Models;
using marketplace_practice.Services.dto;
using marketplace_practice.Services.interfaces;
using marketplace_practice.Utils;

namespace marketplace_practice.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly TokenService _tokenService;
        private readonly AuthUtils _authUtils;
        public UserService(AppDbContext context, TokenService tokenService, AuthUtils authUtils)
        {
            _context = context;
            _tokenService = tokenService;
            _authUtils = authUtils;
        }

        public string GetUserById()
        {
            return "Dev version";
        }

        public CreateUserResultDto CreateUser(CreateUserDto dto)
        {
            try
            {
                if (_context.Users.Any(u => u.Email == dto.Email))
                {
                    throw new Exception("Пользователь с таким email уже существует");
                }
                string passwordHash = _authUtils.HashPassword(dto.Password);

                var roleName = dto.Role.Trim();
                var role = _context.Roles.FirstOrDefault(r => r.Name == roleName);

                if (role == null)
                {
                    throw new ArgumentException($"Роль '{roleName}' не найдена. Допустимые значения: Покупатель, Продавец");
                }

                AccessTokenResult accessTokenData = _tokenService.GenerateAccessToken(123, dto.Email, roleName);
                string refresh_token = _tokenService.GenerateRefreshToken();
                var user = new User
                {
                    Email = dto.Email,
                    ExpiresAt = accessTokenData.ExpiresAt,
                    RefreshToken = refresh_token,
                    PasswordHash = passwordHash,
                    IsActive = true,
                    IsVerified = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    FirstName = dto.FirstName ?? string.Empty,
                    LastName = dto.LastName ?? string.Empty,
                    Roles = new List<Role>()
                };

                user.Roles.Add(role);

                _context.Users.Add(user);
                _context.SaveChanges();

                return new CreateUserResultDto
                {
                    AccessToken = accessTokenData.AccessToken,
                    ExpiresAt = accessTokenData.ExpiresAt,
                    RefreshToken = refresh_token,
                    User = new UserDto(user),
                };
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Ошибка при создании пользователя: {ex.Message}", ex);
            }
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
