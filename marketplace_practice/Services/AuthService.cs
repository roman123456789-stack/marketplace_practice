using marketplace_practice.Controllers.dto;
using marketplace_practice.Models;
using marketplace_practice.Services.dto;
using marketplace_practice.Services.interfaces;
using marketplace_practice.Services.service_models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Net;
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

        public async Task<RegisterResultDto> RegisterAsync(RegisterDto dto)
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
                RefreshTokenModel refreshTokenData = _tokenService.GenerateRefreshToken(); // <--- Потом убрать

                //user.ExpiresAt = accessTokenData.ExpiresAt;
                user.RefreshToken = refreshTokenData.Token;

                // Создание пользователя (без подтверждения email)
                var result = await _userManager.CreateAsync(user, dto.Password);
                if (result.Succeeded)
                {
                    // Генерация токена подтверждения email и его отправка на почту
                    //var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    //await _emailService.SendEmailConfirmationAsync(dto.Email, user.FirstName, user.Id, token);

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

                AccessTokenResult accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email, dto.Role, false);
                // Создание DTO
                return new RegisterResultDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshTokenData,
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
            var (accessToken, refreshTokenData) = await GenerateAndStoreTokensAsync(user);

            return new AuthResultDto
            {
                SignInResult = signInResult,
                AccessTokenResult = accessToken,
                RefreshToken = refreshTokenData,
            };
        }

        private async Task<(AccessTokenResult AccessToken, RefreshTokenModel RefreshToken)> GenerateAndStoreTokensAsync(User user)
        {
            // Получение роли/ролей пользователя
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault(); // над этим еще нужно подумать

            // Генерация токенов доступа
            var accessTokenResult = _tokenService.GenerateAccessToken(user.Id, user.Email, role);
            var refreshTokenData = _tokenService.GenerateRefreshToken();

            // Сохраниение refresh-токеноа в БД
            user.RefreshToken = refreshTokenData.Token;
            user.ExpiresAt = refreshTokenData.ExpiresAt;
            await _userManager.UpdateAsync(user);

            return (accessTokenResult, refreshTokenData);
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
            var newRefreshTokenData = _tokenService.GenerateRefreshToken();

            // Сохраниение refresh-токеноа в БД
            user.RefreshToken = newRefreshTokenData.Token;
            user.ExpiresAt = newRefreshTokenData.ExpiresAt;
            await _userManager.UpdateAsync(user);

            return new AuthResultDto
            {
                AccessTokenResult = newAccessToken,
                RefreshToken = newRefreshTokenData
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
                //await _userManager.UpdateAsync(user); // <--- RefreshToken NOT NULL
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Ошибка при выходе из аккаунта: {ex.Message}", ex);
            }
        }

        public async Task<RecoveryResultDto> RecoveryAsync(string email)
        {
            try
            {
                // Проверка пользователя
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    // Намеренно не сообщаем, что пользователь не найден
                    return new RecoveryResultDto { Success = true };
                }

                // Генерация токена сброса пароля
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                // Отправка email
                //await _emailService.SendPasswordResetEmailAsync(email, user.FirstName, token);

                return new RecoveryResultDto
                {
                    Success = true,
                    ResetToken = token
                };
            }
            catch (Exception ex)
            {
                return new RecoveryResultDto
                {
                    Success = false,
                    ErrorMessage = $"Произошла ошибка при обработке запроса: {ex.Message}"
                };
            }
        }

        public async Task<RecoveryResultDto> ResetPasswordAsync(string email, string token, string newPassword)
        {
            try
            {
                // Проверка пользователя
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return new RecoveryResultDto
                    {
                        Success = false,
                        ErrorMessage = "Неверный токен или email"
                    };
                }

                // Сброс пароля
                var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return new RecoveryResultDto
                    {
                        Success = false,
                        ErrorMessage = errors
                    };
                }

                // Инвалидация старых токенов
                await _userManager.UpdateSecurityStampAsync(user); 

                return new RecoveryResultDto { Success = true };
            }
            catch (Exception ex)
            {
                return new RecoveryResultDto
                {
                    Success = false,
                    ErrorMessage = $"Произошла ошибка при сбросе пароля: {ex.Message}"
                };
            }
        }

        public async Task<RecoveryResultDto> InitiateEmailChangeAsync(string userId, string newEmail)
        {
            try
            {
                // Проверка пользователя
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new RecoveryResultDto
                    {
                        Success = false,
                        ErrorMessage = "Пользователь не найден"
                    };
                }

                // Генерация токена изменения email
                var token = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);

                // Отправка письма на новый email
                // await _emailService.SendEmailChangeConfirmationAsync(newEmail, user.FirstName, user.Id, token);

                return new RecoveryResultDto { Success = true, ResetToken = token };
            }
            catch (Exception ex)
            {
                return new RecoveryResultDto
                {
                    Success = false,
                    ErrorMessage = $"Произошла ошибка при обработке запроса: {ex.Message}"
                };
            }
        }

        public async Task<RecoveryResultDto> ConfirmEmailChangeAsync(string userId, string newEmail, string token)
        {
            try
            {
                // Проверка пользователя
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new RecoveryResultDto
                    {
                        Success = false,
                        ErrorMessage = "Неверный токен или пользователь"
                    };
                }

                // Подтверждение изменения email
                var result = await _userManager.ChangeEmailAsync(user, newEmail, token);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return new RecoveryResultDto
                    {
                        Success = false,
                        ErrorMessage = errors
                    };
                }

                // Обновляем имя пользователя (если используется email как username)
                user.UserName = newEmail;
                await _userManager.UpdateAsync(user);

                // Инвалидация всех токенов
                await _userManager.UpdateSecurityStampAsync(user);

                return new RecoveryResultDto { Success = true };
            }
            catch (Exception ex)
            {
                return new RecoveryResultDto
                {
                    Success = false,
                    ErrorMessage = $"Произошла ошибка при подтверждении email: {ex.Message}"
                };
            }
        }
    }
}
