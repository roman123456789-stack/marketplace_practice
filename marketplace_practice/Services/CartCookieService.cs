using marketplace_practice.Services.interfaces;
using marketplace_practice.Services.service_models;
using System.Security.Claims;
using System.Text.Json;

namespace marketplace_practice.Services
{
    public class CartCookieService : ICartCookieService
    {
        private const string CartCookieName = "CartItems";
        private readonly ICartService _cartService;

        public CartCookieService(ICartService cartService)
        {
            _cartService = cartService;
        }

        public async Task<Result<string>> AddToCartCookieAsync(string productId, int quantity, HttpContext httpContext)
        {
            // Валидация productId
            if (!long.TryParse(productId, out var id))
            {
                return Result<string>.Failure("Неверный формат ID товара");
            }

            // Получаем текущую корзину из куки
            var cartItems = await GetCartFromCookieAsync(httpContext);

            // Ищем товар в корзине
            var existingItem = cartItems.Value?.FirstOrDefault(x => x.ProductId == id);

            if (existingItem != null)
            {
                // Обновляем количество
                existingItem.Quantity += quantity;
            }
            else
            {
                // Добавляем новый товар
                cartItems.Value?.Add(new CartCookieItem
                {
                    ProductId = id,
                    Quantity = quantity,
                });
            }

            // Сохраняем обновленную корзину в куки
            SaveCartToCookie(httpContext, cartItems.Value);

            return Result<string>.Success("Товар добавлен в корзину");
        }

        public async Task<Result<List<CartCookieItem>>> GetCartFromCookieAsync(HttpContext httpContext)
        {
            var cookieValue = httpContext.Request.Cookies[CartCookieName];

            if (string.IsNullOrEmpty(cookieValue))
            {
                return Result<List<CartCookieItem>>.Success(new List<CartCookieItem>());
            }

            try
            {
                var items = JsonSerializer.Deserialize<List<CartCookieItem>>(cookieValue);
                return Result<List<CartCookieItem>>.Success(items ?? new List<CartCookieItem>());
            }
            catch
            {
                return Result<List<CartCookieItem>>.Success(new List<CartCookieItem>());
            }
        }

        private void SaveCartToCookie(HttpContext httpContext, List<CartCookieItem> items)
        {
            var serialized = JsonSerializer.Serialize(items);
            var options = new CookieOptions
            {
                Expires = DateTime.Now.AddDays(7),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            };

            httpContext.Response.Cookies.Append(CartCookieName, serialized, options);
        }

        public async Task<Result<string>> TransferCartToUserAsync(HttpContext httpContext, ClaimsPrincipal user)
        {
            var cartItemsResult = await GetCartFromCookieAsync(httpContext);

            if (!cartItemsResult.IsSuccess || !cartItemsResult.Value.Any())
            {
                return Result<string>.Success("Корзина пуста");
            }

            var errors = new List<string>();

            // Переносим каждый товар в БД
            foreach (var item in cartItemsResult.Value)
            {
                var result = await _cartService.AddCartItemAsync(
                    user,
                    item.ProductId.ToString(),
                    item.Quantity);

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
            await ClearCartCookieAsync(httpContext);

            return Result<string>.Success("Корзина успешно перенесена");
        }

        public async Task RemoveFromCartCookieAsync(string productId, HttpContext httpContext)
        {
            if (!long.TryParse(productId, out var id))
            {
                return; // Невалидный ID - просто выходим
            }

            var cartItems = await GetCartFromCookieAsync(httpContext);

            if (cartItems.IsSuccess)
            {
                var itemToRemove = cartItems.Value?.FirstOrDefault(x => x.ProductId == id);
                if (itemToRemove != null)
                {
                    cartItems.Value?.Remove(itemToRemove);
                    SaveCartToCookie(httpContext, cartItems.Value);
                }
            }
        }

        public Task ClearCartCookieAsync(HttpContext httpContext)
        {
            httpContext.Response.Cookies.Delete(CartCookieName);
            return Task.CompletedTask;
        }
    }
}
