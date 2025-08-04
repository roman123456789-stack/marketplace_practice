using marketplace_practice.Controllers.dto;
using marketplace_practice.Models;
using marketplace_practice.Services.dto;
using marketplace_practice.Services.interfaces;
using marketplace_practice.Utils;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace marketplace_practice.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly TokenService _tokenService;
        private readonly AuthUtils _authUtils;
        public UserService(
            AppDbContext context, 
            UserManager<User> userManager, 
            RoleManager<Role> roleManager, 
            TokenService tokenService, 
            AuthUtils authUtils)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
            _authUtils = authUtils;
        }

        public string GetUserById()
        {
            return "Dev version";
        }

        public async Task<CreateUserResultDto> CreateUserAsync(CreateUserDto dto)
        {
            try
            {
                var existingUser = await _userManager.FindByEmailAsync(dto.Email);
                if (existingUser != null)
                {
                    throw new Exception("Пользователь с таким email уже существует");
                }

                var role = await _roleManager.FindByNameAsync(dto.Role.Trim().ToUpper());
                if (role == null)
                {
                    throw new ArgumentException($"Роль '{dto.Role}' не найдена. Допустимые значения: Покупатель, Продавец");
                }

                var user = new User
                {
                    UserName = dto.Email, // Обязательное поле для Identity
                    Email = dto.Email,
                    FirstName = dto.FirstName ?? string.Empty,
                    LastName = dto.LastName ?? string.Empty,
                    IsActive = true,
                    IsVerified = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var accessTokenData = _tokenService.GenerateAccessToken(user.Id, user.Email, role.Name);
                var refreshToken = _tokenService.GenerateRefreshToken();

                user.RefreshToken = refreshToken;
                user.ExpiresAt = accessTokenData.ExpiresAt;

                var result = await _userManager.CreateAsync(user, dto.Password);
                if (!result.Succeeded)
                {
                    throw new Exception($"Ошибка при создании пользователя: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }

                var roleResult = await _userManager.AddToRoleAsync(user, role.Name);
                if (!roleResult.Succeeded)
                {
                    throw new Exception($"Ошибка при назначении роли: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                }

                await _userManager.UpdateAsync(user);

                return new CreateUserResultDto
                {
                    AccessToken = accessTokenData.AccessToken,
                    ExpiresAt = accessTokenData.ExpiresAt,
                    RefreshToken = refreshToken,
                    User = new UserDto(user)
                    {
                        Roles = new List<RoleDto>
                        {
                            new RoleDto
                                {
                                Id = role.Id,
                                Name = role.Name,
                                Description = role.Description
                            }
                        }.ToList()
                    }
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
