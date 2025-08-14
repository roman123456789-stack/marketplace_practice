using marketplace_practice.Services.dto.Users;
using marketplace_practice.Services.service_models;
using System.Security.Claims;

namespace marketplace_practice.Services.interfaces
{
    public interface IUserService
    {
        public Task<Result<(UserDto, string)>> CreateUserAsync(
            string email,
            string password,
            string userRole,
            string? firstName,
            string? lastName);

        public Task<Result<UserDto>> GetUserByIdAsync(ClaimsPrincipal userPrincipal, string userId);

        public Task<Result<UserDto>> UpdateUserAsync(
            ClaimsPrincipal userPrincipal,
            string userId,
            string? firstName,
            string? lastName,
            string? phoneNumber);

        public Task<Result<string>> DeleteUserAsync(ClaimsPrincipal userPrincipal, string userId);
    }
}
