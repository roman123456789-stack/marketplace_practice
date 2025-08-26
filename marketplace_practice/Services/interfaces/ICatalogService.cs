using marketplace_practice.Controllers.dto.Products;
using marketplace_practice.Services.dto.Catalog;
using marketplace_practice.Services.dto.Products;
using marketplace_practice.Services.service_models;
using System.Security.Claims;

namespace marketplace_practice.Services.interfaces
{
    public interface ICatalogService
    {
        public Task<Result<CategoryHierarchyDto>> AddCategoryAsync(CategoryHierarchyDto categoryHierarchy);

        public Task<Result<ICollection<CategoryHierarchiesDto>>> GetCategoryHierarchiesAsync();

        public Task<Result<ICollection<ProductDto>>> GetProductsFromCategory(
            ClaimsPrincipal userPrincipal,
            HttpContext httpContext,
            string[] pathSegments);
    }
}
