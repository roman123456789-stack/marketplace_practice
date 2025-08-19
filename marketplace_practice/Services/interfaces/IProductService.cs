using marketplace_practice.Models.Enums;
using marketplace_practice.Services.dto.Products;
using marketplace_practice.Controllers.dto.Products;
using marketplace_practice.Services.service_models;
using System.Security.Claims;

namespace marketplace_practice.Services.interfaces
{
    public interface IProductService
    {
        public Task<Result<ProductDto>> CreateProductAsync(
            ClaimsPrincipal userPrincipal,
            string name,
            string? description,
            decimal price,
            decimal? promotionalPrice,
            short? size,
            Currency currency,
            ICollection<CategoryHierarchyDto> categoryHierarchies,
            ICollection<string>? imagesUrl,
            int stockQuantity = 0);

        public Task<Result<ProductDto>> GetProductByIdAsync(ClaimsPrincipal userPrincipal, string productId);

        public Task<Result<ProductDto>> UpdateProductAsync(
            ClaimsPrincipal userPrincipal,
            string productId,
            string? name,
            string? description,
            decimal? price,
            decimal? promotionalPrice,
            short? size,
            Currency? currency,
            ICollection<CategoryHierarchyDto>? categoryHierarchies,
            ICollection<string>? imagesUrl,
            int? stockQuantity);

        public Task<Result<string>> DeleteProductAsync(ClaimsPrincipal userPrincipal, string productId);

        public Task<Result<ICollection<ProductDto>>> GetProductListAsync(
            ClaimsPrincipal userPrincipal,
            string? targetUserId = null);
    }
}
