using marketplace_practice.Models;
using marketplace_practice.Services.dto.Auth;
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
        private readonly SignInManager<User> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;

        public AuthService(
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            SignInManager<User> signInManager,
            ITokenService tokenService,
            IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _emailService = emailService;
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
            catch
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
            catch
            {
                throw;
            }
        }

        private async Task<AuthTokensDto?> GenerateAndStoreTokensAsync(User user)
        {
            // Получение роли/ролей пользователя
            var roles = await _userManager.GetRolesAsync(user);

            // Генерация токенов доступа
            var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email!, roles);
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

                // Проверка, нужно ли обновлять refresh-токен (осталось < 1 дня)
                bool shouldRefreshToken = user.ExpiresAt.HasValue
                    && (user.ExpiresAt.Value - DateTime.UtcNow).TotalDays < 1;

                // Генерация токенов
                AuthTokensDto? authTokens;
                if (shouldRefreshToken)
                {
                    // Генерация новых токенов (включая refresh-токен)
                    authTokens = await GenerateAndStoreTokensAsync(user);
                }
                else
                {
                    // Генерация только нового access-токена (refresh-токен остается прежним)
                    var roles = await _userManager.GetRolesAsync(user);
                    var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email!, roles);

                    authTokens = new AuthTokensDto
                    {
                        AccessToken = accessToken,
                        RefreshToken = new Token
                        {
                            Value = token,
                            ExpiresAt = (DateTime)user.ExpiresAt!
                        }
                    };
                }

                if (authTokens == null)
                    return Result<AuthTokensDto>.Failure("Ошибка генерации токенов");

                return Result<AuthTokensDto>.Success(authTokens);
            }
            catch
            {
                throw;
            }
        }

        public async Task<Result<string>> LogoutAsync(ClaimsPrincipal userPrincipal)
        {
            try
            {
                // Выход из системы (удаляет аутентификационные куки)
                await _signInManager.SignOutAsync();

                // Проверка пользователя
                var user = await _userManager.GetUserAsync(userPrincipal);
                if (user == null)
                {
                    return Result<string>.Failure("Пользователь не найден");
                }

                // Инвалидация refresh-токена
                user.RefreshToken = null;
                await _userManager.UpdateAsync(user);

                return Result<string>.Success(string.Empty);
            }
            catch
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
                    return Result<string>.Failure("Пользователь не найден");
                }

                // Генерация токена сброса пароля
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                // Отправка email
                // await _emailService.SendPasswordResetEmailAsync(email, user.FirstName, token);

                return Result<string>.Success(token);
            }
            catch
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
            catch
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
            catch
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
            catch
            {
                throw;
            }
        }
    }
}
