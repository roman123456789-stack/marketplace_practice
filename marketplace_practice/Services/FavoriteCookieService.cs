using marketplace_practice.Services.interfaces;
using marketplace_practice.Services.service_models;
using System.Security.Claims;
using System.Text.Json;

namespace marketplace_practice.Services
{
    public class FavoriteCookieService : IFavoriteCookieService
    {
        private const string FavoritesCookieName = "FavoriteItems";
        private readonly IFavoriteProductService _favoriteProductService;

        public FavoriteCookieService(IFavoriteProductService favoriteProductService)
        {
            _favoriteProductService = favoriteProductService;
        }

        public async Task<Result<string>> AddToFavoritesCookieAsync(string productId, HttpContext httpContext)
        {
            // Валидация productId
            if (!long.TryParse(productId, out _))
            {
                return Result<string>.Failure("Неверный формат ID товара");
            }

            // Получаем текущее избранное из куки
            var favoriteItems = await GetFavoritesFromCookieAsync(httpContext);

            // Проверяем, не добавлен ли уже товар
            if (favoriteItems.Value?.Contains(productId) == true)
            {
                return Result<string>.Failure("Товар уже в избранном");
            }

            // Добавляем новый товар
            favoriteItems.Value?.Add(productId);

            // Сохраняем обновленное избранное в куки
            SaveFavoritesToCookie(httpContext, favoriteItems.Value);

            return Result<string>.Success("Товар добавлен в избранное");
        }

        public async Task<Result<List<string>>> GetFavoritesFromCookieAsync(HttpContext httpContext)
        {
            var cookieValue = httpContext.Request.Cookies[FavoritesCookieName];

            if (string.IsNullOrEmpty(cookieValue))
            {
                return Result<List<string>>.Success(new List<string>());
            }

            try
            {
                var items = JsonSerializer.Deserialize<List<string>>(cookieValue);
                return Result<List<string>>.Success(items ?? new List<string>());
            }
            catch
            {
                return Result<List<string>>.Success(new List<string>());
            }
        }

        private void SaveFavoritesToCookie(HttpContext httpContext, List<string> items)
        {
            var serialized = JsonSerializer.Serialize(items);
            var options = new CookieOptions
            {
                Expires = DateTime.Now.AddDays(7),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            };

            httpContext.Response.Cookies.Append(FavoritesCookieName, serialized, options);
        }

        public async Task<Result<string>> TransferFavoritesToUserAsync(HttpContext httpContext, ClaimsPrincipal user)
        {
            var favoriteItemsResult = await GetFavoritesFromCookieAsync(httpContext);

            if (!favoriteItemsResult.IsSuccess || !favoriteItemsResult.Value.Any())
            {
                return Result<string>.Success("Избранное пусто");
            }

            var errors = new List<string>();

            // Переносим каждый товар в БД
            foreach (var productId in favoriteItemsResult.Value)
            {
                var result = await _favoriteProductService.AddToFavoritesAsync(user, productId);

                if (!result.IsSuccess)
                {
                    errors.AddRange(result.Errors);
                }
            }

            if (errors.Any())
            {
                return Result<string>.Failure(errors);
            }

            // Очищаем куки после переноса
            await ClearFavoritesCookieAsync(httpContext);

            return Result<string>.Success("Избранное успешно перенесено");
        }

        public async Task RemoveFromFavoritesCookieAsync(string productId, HttpContext httpContext)
        {
            var favoriteItems = await GetFavoritesFromCookieAsync(httpContext);

            if (favoriteItems.IsSuccess)
            {
                favoriteItems.Value?.Remove(productId);
                SaveFavoritesToCookie(httpContext, favoriteItems.Value);
            }
        }

        public Task ClearFavoritesCookieAsync(HttpContext httpContext)
        {
            httpContext.Response.Cookies.Delete(FavoritesCookieName);
            return Task.CompletedTask;
        }
    }
}
