using marketplace_practice.Services.dto.Products;
using marketplace_practice.Services.service_models;
using System.Security.Claims;

namespace marketplace_practice.Services.interfaces
{
    public interface IFavoriteProductService
    {
        public Task<Result<string>> AddToFavoritesAsync(ClaimsPrincipal userPrincipal, string productId);

        public Task<Result<ICollection<ProductDto>>> GetFavoritesAsync(
            ClaimsPrincipal userPrincipal,
            string? targetUserId = null);

        public Task<Result<string>> RemoveFromFavoritesAsync(ClaimsPrincipal userPrincipal, string productId);
    }
}
