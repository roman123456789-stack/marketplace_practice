using marketplace_practice.Models;
using marketplace_practice.Services.dto.Carts;
using marketplace_practice.Services.dto.Products;
using marketplace_practice.Services.interfaces;
using marketplace_practice.Services.service_models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace marketplace_practice.Services
{
    public class CartService : ICartService
    {
        private readonly AppDbContext _appDbContext;

        public CartService(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<Result<string>> AddCartItemAsync(
            ClaimsPrincipal userPrincipal,
            string productId)
        {
            // Валидация ID товара
            if (!long.TryParse(productId, out var id))
            {
                return Result<string>.Failure("Неверный формат ID товара");
            }

            // Получение пользователя из ClaimsPrincipal
            var userId = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out var buyerId))
            {
                return Result<string>.Failure("Не удалось идентифицировать пользователя");
            }

            // Проверка существования товара
            if (!await _appDbContext.Products.AnyAsync(p => p.Id == id))
            {
                return Result<string>.Failure("Указанный товар не существует");
            }

            // Начало транзакции
            using var transaction = await _appDbContext.Database.BeginTransactionAsync();

            try
            {
                // Поиск существующей корзины пользователя
                var cart = await _appDbContext.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == buyerId);

                // Создание новой корзины, если не существует
                if (cart == null)
                {
                    cart = new Cart
                    {
                        UserId = buyerId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _appDbContext.Carts.Add(cart);
                }

                // Поиск существующего товара в корзине
                var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == id);

                if (existingItem != null)
                {
                    return Result<string>.Failure("Данный товар уже в корзине");
                }
                else
                {
                    // Добавление нового товара
                    cart.CartItems.Add(new CartItem
                    {
                        ProductId = id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                await _appDbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return Result<string>.Success(string.Empty);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Result<ICollection<CartItemDto>>> GetCartAsync(
            ClaimsPrincipal userPrincipal,
            string? targetUserId = null)
        {
            // Получение пользователя из ClaimsPrincipal
            var userId = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out var currentUserId))
            {
                return Result<ICollection<CartItemDto>>.Failure("Не удалось идентифицировать пользователя");
            }

            // Определение ID целевого пользователя
            long? resolvedUserId = null;

            if (!string.IsNullOrEmpty(targetUserId))
            {
                // Проверка прав администратора
                if (userId != targetUserId && !userPrincipal.IsInRole("MainAdmin"))
                {
                    return Result<ICollection<CartItemDto>>.Failure("Отказано в доступе");
                }

                // Преобразование строки в long
                if (!long.TryParse(targetUserId, out var parsedUserId))
                {
                    return Result<ICollection<CartItemDto>>.Failure("Некорректный формат ID пользователя");
                }
                resolvedUserId = parsedUserId;
            }
            else
            {
                resolvedUserId = currentUserId;
            }

            try
            {
                var cartItems = await _appDbContext.CartItems
                    .AsNoTracking()
                    .Where(ci => ci.Cart.UserId == resolvedUserId)
                    .OrderByDescending(ci => ci.CreatedAt)
                    .Select(ci => new CartItemDto
                    {
                        CartItemId = ci.Id.ToString(),
                        productBriefInfo = new ProductBriefInfoDto
                        {
                            Id = ci.Product.Id,
                            UserId = ci.Product.UserId,
                            Name = ci.Product.Name,
                            Price = ci.Product.Price,
                            Currency = ci.Product.Currency
                        },
                        CreatedAt = ci.CreatedAt,
                        UpdatedAt = ci.UpdatedAt
                    })
                    .ToListAsync();

                return Result<ICollection<CartItemDto>>.Success(cartItems);
            }
            catch
            {
                throw;
            }
        }

        public async Task<Result<string>> DeleteCartItemAsync(
            ClaimsPrincipal userPrincipal,
            string cartItemId)
        {
            // Валидация параметров
            if (!long.TryParse(cartItemId, out var id))
                return Result<string>.Failure("Неверный формат ID элемента корзины'");

            // Получение пользователя из ClaimsPrincipal
            var userId = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out var currentUserId))
            {
                return Result<string>.Failure("Не удалось идентифицировать пользователя");
            }

            try
            {
                // Поиск элемента корзины с проверкой владельца
                var cartItem = await _appDbContext.CartItems
                    .Include(ci => ci.Cart)
                    .FirstOrDefaultAsync(ci =>
                        ci.Id == id);

                if (cartItem == null)
                {
                    return Result<string>.Failure("Товар не найден в корзине");
                }

                // Проверка прав доступа
                if (cartItem.Cart.UserId != currentUserId)
                {
                    return Result<string>.Failure("Отказано в доступе");
                }

                // Удаление элемента
                _appDbContext.CartItems.Remove(cartItem);
                await _appDbContext.SaveChangesAsync();

                return Result<string>.Success(string.Empty);
            }
            catch
            {
                throw;
            }
        }
    }
}
