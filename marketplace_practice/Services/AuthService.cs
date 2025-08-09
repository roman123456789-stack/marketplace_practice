using marketplace_practice.Models;
using marketplace_practice.Services.dto;
using marketplace_practice.Services.interfaces;
using marketplace_practice.Services.service_models;
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

        public async Task<Result<RegisterResultDto>> RegisterAsync(
            string email,
            string password,
            string userRole,
            string? firstName,
            string? lastName)
        {
            try
            {
                // Проверка пользователя
                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null)
                {
                    return Result<RegisterResultDto>.Failure("Пользователь с таким email уже существует");
                }

                // Проверка роли
                var role = await _roleManager.FindByNameAsync(userRole.Trim().ToUpper());
                if (role == null)
                {
                    return Result<RegisterResultDto>.Failure($"Роль '{userRole}' не найдена. Допустимые значения: Покупатель, Продавец");
                }

                // Создание пользователя
                var user = new User
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName ?? string.Empty,
                    LastName = lastName ?? string.Empty,
                    IsActive = true,
                    IsVerified = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Генерация токенов доступа
                //var accessTokenData = _tokenService.GenerateAccessToken(user.Id, user.Email, role.Name);
                Token refreshToken = _tokenService.GenerateRefreshToken(); // <--- Потом убрать

                //user.ExpiresAt = accessTokenData.ExpiresAt;
                user.RefreshToken = refreshToken.Value;

                // Создание пользователя (без подтверждения email)
                var createUserResult = await _userManager.CreateAsync(user, password);
                if (createUserResult.Succeeded)
                {
                    // Генерация токена подтверждения email и его отправка на почту
                    //var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    //await _emailService.SendEmailConfirmationAsync(dto.Email, user.FirstName, user.Id, token);

                    // Для корректной работы нужно настроить "EmailConfig" в appsettings.json
                }
                else
                {
                    return Result<RegisterResultDto>.Failure(createUserResult.Errors.Select(e => e.Description));
                }

                // Назначение роли пользователю
                var addRoleResult = await _userManager.AddToRoleAsync(user, role.Name);
                if (!addRoleResult.Succeeded)
                {
                    return Result<RegisterResultDto>.Failure(addRoleResult.Errors.Select(e => e.Description));
                }

                await _userManager.UpdateAsync(user);

                var _loyaltyAccount = await _loyaltyService.GetOrCreateAccount(user.Id);

                Token accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email, userRole, false);

                // Создание DTO
                return Result<RegisterResultDto>.Success(new RegisterResultDto
                {
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
                });
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Result<AuthTokensDto>> ConfirmEmailAndSignInAsync(string userId, string token)
        {
            try
            {
                // Проверка пользователя
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Result<AuthTokensDto>.Failure("Пользователь не найден");
                }

                // Подтверждение email
                var confirmationResult = await _userManager.ConfirmEmailAsync(user, token);
                if (!confirmationResult.Succeeded)
                {
                    return Result<AuthTokensDto>.Failure(confirmationResult.Errors.Select(e => e.Description));
                }

                // Вход в аккаунт
                await _signInManager.SignInAsync(user, isPersistent: true);

                // Генерация токенов
                var authTokens = await GenerateAndStoreTokensAsync(user);
                if (authTokens == null)
                {
                    return Result<AuthTokensDto>.Failure("Ошибка генерации токенов");
                }

                // Обновление флага верификации
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);

                return Result<AuthTokensDto>.Success(authTokens);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Result<AuthTokensDto>> LoginAsync(string email, string password, bool rememberMe)
        {
            try
            {
                // Проверка пользователя
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return Result<AuthTokensDto>.Failure("Неверные учетные данные");
                }

                if (!user.EmailConfirmed)
                {
                    return Result<AuthTokensDto>.Failure("Подтвердите email перед входом");
                }

                // Попытка входа
                var signInResult = await _signInManager.PasswordSignInAsync(
                    user,
                    password,
                    rememberMe,
                    lockoutOnFailure: false);

                if (!signInResult.Succeeded)
                {
                    return HandleSignInResult(signInResult, user);
                }

                // Генерация токенов
                var authTokens = await GenerateAndStoreTokensAsync(user);
                if (authTokens == null)
                    return Result<AuthTokensDto>.Failure("Ошибка генерации токенов");

                return Result<AuthTokensDto>.Success(authTokens);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task<AuthTokensDto?> GenerateAndStoreTokensAsync(User user)
        {
            // Получение роли/ролей пользователя
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault(); // над этим еще нужно подумать

            // Генерация токенов доступа
            var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email, role);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // Сохраниение refresh-токена в БД
            user.RefreshToken = refreshToken.Value;
            user.ExpiresAt = refreshToken.ExpiresAt;
            await _userManager.UpdateAsync(user);

            return accessToken != null && refreshToken != null
                ? new AuthTokensDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                }
                : null;
        }

        private Result<AuthTokensDto> HandleSignInResult(SignInResult result, User user)
        {
            if (result.IsLockedOut)
            {
                return Result<AuthTokensDto>.Failure("Аккаунт временно заблокирован");
            }

            if (result.RequiresTwoFactor)
            {
                return Result<AuthTokensDto>.Failure("Требуется двухфакторная аутентификация");
            }

            return Result<AuthTokensDto>.Failure("Неверные учетные данные");
        }

        public async Task<Result<AuthTokensDto>> RefreshTokenAsync(string token)
        {
            try
            {
                // Проверка пользователя
                var user = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.RefreshToken == token);

                if (user == null)
                {
                    return Result<AuthTokensDto>.Failure("Пользователь не найден");
                }

                // Проверка refresh-токена
                if (user.ExpiresAt < DateTime.UtcNow)
                {
                    return Result<AuthTokensDto>.Failure("Время жизни токена истекло");
                }

                // Генерация токенов
                var authTokens = await GenerateAndStoreTokensAsync(user);
                if (authTokens == null)
                    return Result<AuthTokensDto>.Failure("Ошибка генерации токенов");

                return Result<AuthTokensDto>.Success(authTokens);
            }
            catch (Exception ex)
            {
                throw;
            }
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
                throw;
            }
        }

        public async Task<Result<string>> RecoveryAsync(string email)
        {
            try
            {
                // Проверка пользователя
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    // Намеренно не сообщаем, что пользователь не найден
                    return Result<string>.Success(string.Empty);
                }

                // Генерация токена сброса пароля
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                // Отправка email
                //await _emailService.SendPasswordResetEmailAsync(email, user.FirstName, token);

                return Result<string>.Success(token);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Result<string>> ResetPasswordAsync(string email, string token, string newPassword)
        {
            try
            {
                // Проверка пользователя
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return Result<string>.Failure("Пользователь не найден");
                }

                // Сброс пароля
                var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

                if (!result.Succeeded)
                {
                    return Result<string>.Failure(result.Errors.Select(e => e.Description));
                }

                // Инвалидация старых токенов
                await _userManager.UpdateSecurityStampAsync(user);

                return Result<string>.Success(string.Empty);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Result<string>> InitiateEmailChangeAsync(string userId, string newEmail)
        {
            try
            {
                // Проверка пользователя
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Result<string>.Failure("Пользователь не найден");
                }

                if (await _userManager.FindByEmailAsync(newEmail) != null)
                {
                    return Result<string>.Failure("Email уже используется");
                }

                // Генерация токена изменения email
                var token = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);

                // Отправка письма на новый email
                // await _emailService.SendEmailChangeConfirmationAsync(newEmail, user.FirstName, user.Id, token);

                return Result<string>.Success(token);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Result<string>> ConfirmEmailChangeAsync(string userId, string newEmail, string token)
        {
            try
            {
                // Проверка пользователя
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Result<string>.Failure("Пользователь не найден");
                }

                // Подтверждение изменения email
                var result = await _userManager.ChangeEmailAsync(user, newEmail, token);

                if (!result.Succeeded)
                {
                    return Result<string>.Failure(result.Errors.Select(e => e.Description));
                }

                // Обновляем имя пользователя (если используется email как username)
                user.UserName = newEmail;
                await _userManager.UpdateAsync(user);

                // Инвалидация всех токенов
                await _userManager.UpdateSecurityStampAsync(user);

                return Result<string>.Success(token);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
