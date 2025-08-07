using marketplace_practice.Controllers.dto;
using marketplace_practice.Models;
using marketplace_practice.Services.dto;
using marketplace_practice.Services.interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;

namespace marketplace_practice.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly ILoyaltyService _loyaltyService;
        private readonly IEmailService _emailService;

        public AuthService(
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            SignInManager<User> signInManager,
            ITokenService tokenService,
            ILoyaltyService loyaltyService,
            IEmailService emailService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _loyaltyService = loyaltyService;
            _emailService = emailService;
        }

        public async Task<RegisterResultDto> CreateUserAsync(RegisterDto dto)
        {
            try
            {
                // Проверка пользователя
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
                //var accessTokenData = _tokenService.GenerateAccessToken(user.Id, user.Email, role.Name);
                var refreshToken = _tokenService.GenerateRefreshToken();

                //user.ExpiresAt = accessTokenData.ExpiresAt;
                user.RefreshToken = refreshToken;

                // Создание пользователя (без подтверждения email)
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

                // Назначение роли пользователю
                var roleResult = await _userManager.AddToRoleAsync(user, role.Name);
                if (!roleResult.Succeeded)
                {
                    throw new Exception($"Ошибка при назначении роли: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                }

                await _userManager.UpdateAsync(user);

                var _loyaltyAccount = await _loyaltyService.GetOrCreateAccount(user.Id);

                // Создание DTO
                return new RegisterResultDto
                {
                    //AccessToken = accessTokenData.AccessToken,
                    //ExpiresAt = accessTokenData.ExpiresAt,
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

        public async Task<IdentityResult> ConfirmEmailAsync(string userId, string token)
        {
            try
            {
                // Проверка пользователя
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    throw new Exception("Пользователь не найден");
                }

                // Подтверждение email
                var result = await _userManager.ConfirmEmailAsync(user, token);

                if (result.Succeeded)
                {
                    user.IsVerified = true;
                    await _userManager.UpdateAsync(user);
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Ошибка при подтверждении Email: {ex.Message}", ex);
            }
        }

        public async Task<AuthResultDto> LoginAsync(LoginDto loginDto)
        {
            // Проверка пользователя
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
            {
                return new AuthResultDto
                {
                    SignInResult = SignInResult.Failed,
                    ErrorMessage = "Пользователь не найден"
                };
            }

            // Попытка входа
            var signInResult = await _signInManager.PasswordSignInAsync(
                user,
                loginDto.Password,
                loginDto.RememberMe,
                lockoutOnFailure: true);

            if (!signInResult.Succeeded)
            {
                return HandleFailedLogin(signInResult, user);
            }

            // Генерация токенов
            var (accessToken, refreshToken) = await GenerateAndStoreTokensAsync(user);

            return new AuthResultDto
            {
                SignInResult = signInResult,
                AccessTokenResult = accessToken,
                RefreshToken = refreshToken
            };
        }

        private async Task<(AccessTokenResult AccessToken, string RefreshToken)> GenerateAndStoreTokensAsync(User user)
        {
            // Получение роли/ролей пользователя
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault(); // над этим еще нужно подумать

            // Генерация токенов доступа
            var accessTokenResult = _tokenService.GenerateAccessToken(user.Id, user.Email, role);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // Сохраниение refresh-токеноа в БД
            user.RefreshToken = refreshToken;
            user.ExpiresAt = DateTime.UtcNow.AddDays(7);
            await _userManager.UpdateAsync(user);

            return (accessTokenResult, refreshToken);
        }

        private AuthResultDto HandleFailedLogin(SignInResult result, User user)
        {
            // Обработка ошибок взода в аккаунт
            var errorMessage = result switch
            {
                { IsLockedOut: true } => "Аккаунт временно заблокирован",
                { RequiresTwoFactor: true } => "Требуется двухфакторная аутентификация",
                _ => "Неверный email или пароль"
            };

            return new AuthResultDto
            {
                SignInResult = result,
                ErrorMessage = errorMessage
            };
        }

        public async Task<AuthResultDto> RefreshTokenAsync(RefreshTokenDto request)
        {
            // Проверка пользователя
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken);

            if (user == null)
            {
                throw new Exception("Пользователь не найден");
            }

            // Проверка refresh-токена
            if (user.ExpiresAt < DateTime.UtcNow)
            {
                return null;
            }

            // Получение роли/ролей пользователя
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault(); // над этим еще нужно подумать

            // Генерация токенов доступа
            var newAccessToken = _tokenService.GenerateAccessToken(user.Id, user.Email, role);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            // Сохраниение refresh-токеноа в БД
            user.RefreshToken = newRefreshToken;
            user.ExpiresAt = DateTime.UtcNow.AddDays(7);
            await _userManager.UpdateAsync(user);

            return new AuthResultDto
            {
                AccessTokenResult = newAccessToken,
                RefreshToken = newRefreshToken
            };
        }

        public async Task LogoutAsync(ClaimsPrincipal userPrincipal)
        {
            try
            {
                // Выход из системы (удаляет аутентификационные куки)
                await _signInManager.SignOutAsync();

                //// Проверка пользователя
                //var user = await _userManager.GetUserAsync(userPrincipal);
                //if (user == null)
                //{
                //    throw new Exception("Пользователь не найден");
                //}

                //// Инвалидация refresh-токена
                //user.RefreshToken = null;
                //await _userManager.UpdateAsync(user); // <==================== RefreshToken NOT NULL
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Ошибка при выходе из аккаунта: {ex.Message}", ex);
            }
        }
    }
}
