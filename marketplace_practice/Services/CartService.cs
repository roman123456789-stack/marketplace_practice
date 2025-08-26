using marketplace_practice.Models;
using marketplace_practice.Models.Enums;
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
            string productId,
            int quantity = 1)
        {
            // Валидация количества
            if (quantity <= 0)
            {
                return Result<string>.Failure("Количество должно быть больше 0");
            }

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

            // Проверка существования и доступности товара
            var product = await _appDbContext.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (product == null)
            {
                return Result<string>.Failure("Указанный товар не существует или недоступен");
            }

            // Проверка достаточного количества на складе
            if (product.StockQuantity < quantity)
            {
                return Result<string>.Failure($"Недостаточно товара на складе. Доступно: {product.StockQuantity}");
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
                    await _appDbContext.Carts.AddAsync(cart);
                    await _appDbContext.SaveChangesAsync(); // Сохраняем чтобы получить ID
                }

                // Поиск существующего товара в корзине
                var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == id);

                if (existingItem != null)
                {
                    // Увеличение количества существующего товара
                    var newQuantity = existingItem.Quantity + quantity;

                    // Проверка, не превышает ли общее количество доступное на складе
                    if (product.StockQuantity < newQuantity)
                    {
                        await transaction.RollbackAsync();
                        return Result<string>.Failure(
                            $"Нельзя добавить {quantity} шт. товара. " +
                            $"В корзине уже {existingItem.Quantity} шт., доступно на складе: {product.StockQuantity}");
                    }

                    existingItem.Quantity = newQuantity;
                    existingItem.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Добавление нового товара
                    cart.CartItems.Add(new CartItem
                    {
                        ProductId = id,
                        Quantity = quantity,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                cart.UpdatedAt = DateTime.UtcNow;
                await _appDbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return Result<string>.Success("Товар успешно добавлен в корзину");
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
                        Quantity = ci.Quantity,
                        productBriefInfo = new ProductBriefInfoDto
                        {
                            Id = ci.Product.Id,
                            UserId = ci.Product.UserId,
                            Name = ci.Product.Name,
                            Price = ci.Product.Price,
                            PromotionalPrice = ci.Product.PromotionalPrice,
                            Currency = ci.Product.Currency.GetDisplayName(),
                            ProductImages = ci.Product.ProductImages
                            .Select(pi => new ProductImageDto
                            {
                                Url = pi.Url,
                                IsMain = pi.IsMain
                            }).ToList(),
                            CreatedAt = ci.Product.CreatedAt,
                            IsFavirite = ci.Product.FavoriteProducts.Any(fp => fp.UserId == currentUserId),
                            IsAdded = true
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

        public async Task<Result<ICollection<CartItemDto>>> GetCartFromCookieAsync(
            List<CartCookieItem> cartItemsResult,
            List<string> favoriteProductIds = null)
        {
            try
            {
                if (cartItemsResult == null || !cartItemsResult.Any())
                {
                    return Result<ICollection<CartItemDto>>.Success(new List<CartItemDto>());
                }

                // Получение ID товаров из корзины
                var productIds = cartItemsResult.Select(x => x.ProductId).ToList();

                // Конвертируем favoriteProductIds в long
                var favoriteIds = favoriteProductIds?
                    .Select(id => long.TryParse(id, out var productId) ? productId : (long?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id.Value)
                    .ToList() ?? new List<long>();

                // Загрузка информации о товарах
                var products = await _appDbContext.Products
                    .AsNoTracking()
                    .Where(p => productIds.Contains(p.Id) && p.IsActive)
                    .Include(p => p.ProductImages)
                    .Include(p => p.User)
                    .ToDictionaryAsync(p => p.Id);

                // Создание DTO
                var cartItems = new List<CartItemDto>();

                foreach (var cookieItem in cartItemsResult)
                {
                    if (products.TryGetValue(cookieItem.ProductId, out var product))
                    {
                        cartItems.Add(new CartItemDto
                        {
                            CartItemId = $"cookie_{cookieItem.ProductId}",
                            Quantity = cookieItem.Quantity,
                            productBriefInfo = new ProductBriefInfoDto
                            {
                                Id = product.Id,
                                UserId = product.UserId,
                                Name = product.Name,
                                Price = product.Price,
                                PromotionalPrice = product.PromotionalPrice,
                                Currency = product.Currency.GetDisplayName(),
                                ProductImages = product.ProductImages
                                    .Select(pi => new ProductImageDto
                                    {
                                        Url = pi.Url,
                                        IsMain = pi.IsMain
                                    })
                                    .ToList(),
                                CreatedAt = DateTime.UtcNow,
                                IsFavirite = favoriteIds.Contains(product.Id),
                                IsAdded = true
                            },
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                return Result<ICollection<CartItemDto>>.Success(cartItems);
            }
            catch (Exception ex)
            {
                return Result<ICollection<CartItemDto>>.Failure($"Ошибка при получении корзины из куки: {ex.Message}");
            }
        }

        public async Task<Result<string>> DeleteCartItemAsync(
            ClaimsPrincipal userPrincipal,
            string productId)
        {
            // Валидация параметров
            if (!long.TryParse(productId, out var id))
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
                        ci.ProductId == id);

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
