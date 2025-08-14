using marketplace_practice.Models;
using marketplace_practice.Services.dto.Users;
using marketplace_practice.Services.interfaces;
using marketplace_practice.Services.service_models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace marketplace_practice.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _appDbContext;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly ILoyaltyService _loyaltyService;
        private readonly IAuthService _authService;

        public UserService(
            AppDbContext appDbContext,
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            ILoyaltyService loyaltyService,
            IAuthService authService)
        {
            _appDbContext = appDbContext;
            _userManager = userManager;
            _roleManager = roleManager;
            _loyaltyService = loyaltyService;
            _authService = authService;
        }

        public async Task<Result<(UserDto, string)>> CreateUserAsync(
            string email,
            string password,
            string userRole,
            string? firstName,
            string? lastName)
        {
            // Получение DbContext из UserManager
            using var transaction = await _appDbContext.Database.BeginTransactionAsync();

            try
            {
                // Проверка пользователя
                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null)
                {
                    return Result<(UserDto, string)>.Failure("Пользователь с таким email уже существует");
                }

                // Проверка роли
                var role = await _roleManager.FindByNameAsync(userRole.Trim().ToUpper());
                if (role == null)
                {
                    return Result<(UserDto, string)>.Failure($"Роль '{userRole}' не найдена. Допустимые значения: Покупатель, Продавец");
                }

                // Создание пользователя
                var user = new User
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName ?? string.Empty,
                    LastName = lastName ?? string.Empty,
                    IsActive = true,
                    EmailConfirmed = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

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
                    await transaction.RollbackAsync();
                    return Result<(UserDto, string)>.Failure(createUserResult.Errors.Select(e => e.Description));
                }

                // Назначение роли пользователю
                var addRoleResult = await _userManager.AddToRoleAsync(user, role.Name);
                if (!addRoleResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return Result<(UserDto, string)>.Failure(addRoleResult.Errors.Select(e => e.Description));
                }

                // Создание аккаунта лояльности
                var _loyaltyAccount = await _loyaltyService.GetOrCreateAccount(user.Id);

                await _userManager.UpdateAsync(user);

                // Генерация токена подтверждения email (временно)
                var emailVerificationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                await transaction.CommitAsync();
                return Result<(UserDto, string)>.Success((new UserDto(user), emailVerificationToken)); // токен потом нужно убрать
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Result<UserDto>> GetUserByIdAsync(ClaimsPrincipal userPrincipal, string userId)
        {
            try
            {
                // Поиск пользователя
                var user = await _appDbContext.Users
                    .AsNoTracking()
                    .Include(u => u.LoyaltyAccount)
                    .FirstOrDefaultAsync(u => u.Id == long.Parse(userId));

                if (user == null)
                {
                    return Result<UserDto>.Failure("Пользователь не найден");
                }

                // Проверка подлинности и прав
                var isVerified = await VerifyAccessRights(userPrincipal, user);
                if (!isVerified)
                {
                    return Result<UserDto>.Failure("Недостаточно прав для выполнения действия");
                }

                // Загрузка ролей
                var roleNames = await _userManager.GetRolesAsync(user);
                var roles = await _appDbContext.Roles
                    .AsNoTracking()
                    .Where(r => roleNames.Contains(r.Name!))
                    .ToListAsync();

                // Создание RoleDto
                ICollection<RoleDto> rolesDto = new List<RoleDto>();
                foreach (var role in roles)
                {
                    rolesDto.Add(new RoleDto
                    {
                        Name = role.Name!,
                        Description = role.Description
                    });
                }

                return Result<UserDto>.Success(new UserDto(user)
                {
                    Roles = rolesDto,
                    LoyaltyAccount = user.LoyaltyAccount != null
                        ? new LoyaltyAccountDto
                        {
                            Balance = user.LoyaltyAccount.Balance,
                            CreatedAt = user.LoyaltyAccount.CreatedAt,
                            UpdatedAt = user.LoyaltyAccount.UpdatedAt
                        }
                        : null
                });
            }
            catch
            {
                throw;
            }
        }

        public async Task<Result<UserDto>> UpdateUserAsync(
            ClaimsPrincipal userPrincipal,
            string userId,
            string? firstName,
            string? lastName,
            string? phoneNumber)
        {
            using var transaction = await _appDbContext.Database.BeginTransactionAsync();

            try
            {
                // Поиск пользователя
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Result<UserDto>.Failure("Пользователь не найден");
                }

                // Проверка подлинности и прав
                var isVerified = await VerifyAccessRights(userPrincipal, user);
                if (!isVerified)
                {
                    return Result<UserDto>.Failure("Недостаточно прав для выполнения действия");
                }

                // Обновление только измененных полей
                if (!string.IsNullOrEmpty(firstName)) user.FirstName = firstName;
                if (!string.IsNullOrEmpty(lastName)) user.LastName = lastName;
                if (phoneNumber != null) user.PhoneNumber = phoneNumber;
                user.UpdatedAt = DateTime.UtcNow;

                // Сохранение изменений
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return Result<UserDto>.Failure(updateResult.Errors.Select(e => e.Description));
                }

                await transaction.CommitAsync();
                return Result<UserDto>.Success(new UserDto(user));
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Result<string>> DeleteUserAsync(ClaimsPrincipal userPrincipal, string userId)
        {
            using var transaction = await _appDbContext.Database.BeginTransactionAsync();

            try
            {
                // Поиск пользователя
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Result<string>.Failure("Пользователь не найден");
                }

                // Проверка подлинности и прав
                var isVerified = await VerifyAccessRights(userPrincipal, user);
                if (!isVerified)
                {
                    return Result<string>.Failure("Недостаточно прав для выполнения действия");
                }

                // Выход из аккаунта
                var logoutResult = await _authService.LogoutAsync(userPrincipal);
                if (!logoutResult.IsSuccess)
                {
                    return Result<string>.Failure(logoutResult.Errors);
                }

                // Удаление пользователя
                var deleteResult = await _userManager.DeleteAsync(user);
                if (!deleteResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return Result<string>.Failure(deleteResult.Errors.Select(e => e.Description));
                }

                await transaction.CommitAsync();
                return Result<string>.Success(string.Empty);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<bool> VerifyAccessRights(ClaimsPrincipal userPrincipal, User user)
        {
            var currentUserId = _userManager.GetUserId(userPrincipal);
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            if (currentUserId != user.Id.ToString() && !isAdmin)
            {
                return false;
            }

            return true;
        }
    }
}
