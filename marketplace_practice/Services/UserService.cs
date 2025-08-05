using marketplace_practice.Controllers.dto;
using marketplace_practice.Models;
using marketplace_practice.Services.dto;
using marketplace_practice.Services.interfaces;
using Microsoft.AspNetCore.Identity;

namespace marketplace_practice.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly TokenService _tokenService;
        private readonly LoyaltyService _loyaltyService;
        private readonly IEmailService _emailService;

        public UserService(
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            TokenService tokenService,
            LoyaltyService loyaltyService,
            IEmailService emailService
            )
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
            _loyaltyService = loyaltyService;
            _emailService = emailService;
        }

        public string GetUserById()
        {
            return "Dev version";
        }

        public async Task<CreateUserResultDto> CreateUserAsync(CreateUserDto dto)
        {
            try
            {
                // Проверка существующего пользователя
                var existingUser = await _userManager.FindByEmailAsync(dto.Email);
                if (existingUser != null)
                {
                    throw new Exception("Пользователь с таким email уже существует");
                }

                // Проверка роли
                var role = await _roleManager.FindByNameAsync(dto.Role.Trim().ToUpper());
                if (role == null)
                {
                    throw new ArgumentException($"Роль '{dto.Role}' не найдена. Допустимые значения: Покупатель, Продавец");
                }

                // Создание пользователя
                var user = new User
                {
                    UserName = dto.Email,
                    Email = dto.Email,
                    FirstName = dto.FirstName ?? string.Empty,
                    LastName = dto.LastName ?? string.Empty,
                    IsActive = true,
                    IsVerified = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Генерация токенов доступа
                var accessTokenData = _tokenService.GenerateAccessToken(user.Id, user.Email, role.Name);
                var refreshToken = _tokenService.GenerateRefreshToken();

                user.ExpiresAt = accessTokenData.ExpiresAt;
                user.RefreshToken = refreshToken;

                // Создаем пользователя (без подтверждения email)
                var result = await _userManager.CreateAsync(user, dto.Password);
                if (result.Succeeded)
                {
                    // Генерация токена подтверждения email и его отправка на почту
                    //var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    //await _emailService.SendEmailConfirmationAsync(dto.Email, user.Id, token);

                    // Для корректной работы нужно настроить "EmailConfig" в appsettings.json
                }
                else
                {
                    throw new Exception($"Ошибка при создании пользователя: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }

                // Назначаем роль пользователю
                var roleResult = await _userManager.AddToRoleAsync(user, role.Name);
                if (!roleResult.Succeeded)
                {
                    throw new Exception($"Ошибка при назначении роли: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                }

                await _userManager.UpdateAsync(user);

                var _loyaltyAccount = await _loyaltyService.GetOrCreateAccount(user.Id);

                // Создание DTO
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
                        },
                        LoyaltyAccount = new LoyaltyAccountDto
                        {
                            Id = _loyaltyAccount.Id,
                            Balance = _loyaltyAccount.Balance,
                            CreatedAt = _loyaltyAccount.CreatedAt,
                            UpdatedAt = _loyaltyAccount.UpdatedAt
                        }
                    },
                    emailVerificationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user) // временно
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

        public async Task<IdentityResult> ConfirmEmailAsync(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new Exception("Пользователь не найден");
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (result.Succeeded)
            {
                user.IsVerified = true;
                await _userManager.UpdateAsync(user);
            }

            return result;
        }
    }
}
