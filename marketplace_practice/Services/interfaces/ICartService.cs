using marketplace_practice.Services.dto.Carts;
using marketplace_practice.Services.service_models;
using System.Security.Claims;

namespace marketplace_practice.Services.interfaces
{
    public interface ICartService
    {
        public Task<Result<string>> AddCartItemAsync(
            ClaimsPrincipal userPrincipal,
            long productId,
            int quantity = 1);

        public Task<Result<ICollection<CartItemDto>>> GetCartAsync(
            ClaimsPrincipal userPrincipal,
            string? targetUserId = null);

        public Task<Result<string>> DeleteCartItemAsync(
            ClaimsPrincipal userPrincipal,
            string cartItemId);
    }
}
