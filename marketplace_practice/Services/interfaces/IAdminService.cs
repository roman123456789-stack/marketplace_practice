using marketplace_practice.Services.service_models;
using System.Security.Claims;

namespace marketplace_practice.Services.interfaces
{
    public interface IAdminService
    {
        public Task<Result<string>> GiveAdminRoleAsync(ClaimsPrincipal userPrincipal, string targetUserId);
    }
}
