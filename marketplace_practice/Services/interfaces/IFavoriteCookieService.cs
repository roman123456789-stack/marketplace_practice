using marketplace_practice.Services.service_models;
using System.Security.Claims;

namespace marketplace_practice.Services.interfaces
{
    public interface IFavoriteCookieService
    {
        Task<Result<string>> AddToFavoritesCookieAsync(string productId, HttpContext httpContext);
        Task<Result<List<string>>> GetFavoritesFromCookieAsync(HttpContext httpContext);
        Task RemoveFromFavoritesCookieAsync(string productId, HttpContext httpContext);
        Task ClearFavoritesCookieAsync(HttpContext httpContext);
        Task<Result<string>> TransferFavoritesToUserAsync(HttpContext httpContext, ClaimsPrincipal user);
    }
}
