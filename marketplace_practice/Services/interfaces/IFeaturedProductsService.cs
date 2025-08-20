using marketplace_practice.Services.dto.Products;
using marketplace_practice.Services.service_models;
using System.Security.Claims;

namespace marketplace_practice.Services.interfaces
{
    public interface IFeaturedProductsService
    {
        public Task<Result<ICollection<ProductDto>>> GetPopularProductsAsync(
            ClaimsPrincipal userPrincipal,
            int limit = 4);

        public Task<Result<ICollection<ProductDto>>> GetNewProductsAsync(
            ClaimsPrincipal userPrincipal,
            int limit = 4,
            int days = 30);
    }
}
