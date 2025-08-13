using marketplace_practice.Models.Enums;
using marketplace_practice.Services.dto;
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
            Currency Currency,
            string category,
            string? subcategory,
            ICollection<string>? imagesUrl);

        public Task<Result<ProductDto>> GetProductByIdAsync(string productId);

        public Task<Result<ProductDto>> UpdateProductAsync(
            string productId,
            string? name,
            string? description,
            decimal? price,
            Currency? currency,
            string? category,
            string? subcategory,
            ICollection<string>? imagesUrl);

        public Task<Result<string>> DeleteProductAsync(string productId);
    }
}
