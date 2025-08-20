using marketplace_practice.Models;
using marketplace_practice.Services.interfaces;
using marketplace_practice.Services.service_models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace marketplace_practice.Services
{
    public class AdminService : IAdminService
    {
        private readonly UserManager<User> _userManager;

        public AdminService(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public async Task<Result<string>> GiveAdminRoleAsync(ClaimsPrincipal userPrincipal, string targetUserId)
        {
            // Валидация входных параметров
            if (!long.TryParse(targetUserId, out var id))
            {
                return Result<string>.Failure("Неверный формат ID пользователя");
            }

            // Получение текущего пользователя
            var userId = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out var currentUserId))
            {
                return Result<string>.Failure("Не удалось идентифицировать пользователя");
            }

            try
            {
                // Поиск целевого пользователя
                var targetUser = await _userManager.FindByIdAsync(targetUserId);
                if (targetUser == null)
                {
                    return Result<string>.Failure("Пользователь не найден");
                }

                // Проверка, есть ли уже роль у пользователя
                var isAlreadyAdmin = await _userManager.IsInRoleAsync(targetUser, "Admin");
                if (isAlreadyAdmin)
                {
                    return Result<string>.Failure("Пользователь уже имеет роль Admin");
                }

                // Выдача роли
                var result = await _userManager.AddToRoleAsync(targetUser, "Admin");
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return Result<string>.Failure($"Ошибка при выдаче роли: {errors}");
                }

                return Result<string>.Success(string.Empty);
            }
            catch
            {
                throw;
            }
        }
    }
}
