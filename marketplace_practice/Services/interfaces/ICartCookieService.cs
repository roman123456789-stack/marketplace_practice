using marketplace_practice.Services.service_models;
using System.Security.Claims;

namespace marketplace_practice.Services.interfaces
{
    public interface ICartCookieService
    {
        Task<Result<string>> AddToCartCookieAsync(string productId, int quantity, HttpContext httpContext);
        public Task<Result<List<CartCookieItem>>> GetCartFromCookieAsync(HttpContext httpContext);
        Task RemoveFromCartCookieAsync(string productId, HttpContext httpContext);
        Task ClearCartCookieAsync(HttpContext httpContext);
        Task<Result<string>> TransferCartToUserAsync(HttpContext httpContext, ClaimsPrincipal user);
    }
}
