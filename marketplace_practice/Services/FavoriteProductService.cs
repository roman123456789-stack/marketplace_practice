using marketplace_practice.Models;
using marketplace_practice.Services.dto.Products;
using marketplace_practice.Services.dto.Users;
using marketplace_practice.Services.interfaces;
using marketplace_practice.Services.service_models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace marketplace_practice.Services
{
    public class FavoriteProductService : IFavoriteProductService
    {
        private readonly AppDbContext _appDbContext;

        public FavoriteProductService(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<Result<string>> AddToFavoritesAsync(ClaimsPrincipal userPrincipal, string productId)
        {
            // Валидация ID товара
            if (!long.TryParse(productId, out var id))
            {
                return Result<string>.Failure("Неверный формат ID товара");
            }

            // Получение текущего пользователя
            var userId = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out var currentUserId))
            {
                return Result<string>.Failure("Не удалось идентифицировать пользователя");
            }

            using var transaction = await _appDbContext.Database.BeginTransactionAsync();

            try
            {
                // Создание закладки
                var favoriteProduct = new FavoriteProduct
                {
                    UserId = currentUserId,
                    ProductId = id,
                    IsFavorite = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Сохранение закладки
                await _appDbContext.FavoriteProducts.AddAsync(favoriteProduct);
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

        public async Task<Result<ICollection<ProductDto>>> GetFavoritesAsync(
            ClaimsPrincipal userPrincipal,
            string? targetUserId = null)
        {
            // Получение текущего пользователя
            var userId = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out var currentUserId))
            {
                return Result<ICollection<ProductDto>>.Failure("Не удалось идентифицировать пользователя");
            }

            // Определение ID целевого пользователя
            long? resolvedUserId = null;

            if (!string.IsNullOrEmpty(targetUserId))
            {
                // Проверка прав администратора
                if (userId != targetUserId && !userPrincipal.IsInRole("MainAdmin"))
                {
                    return Result<ICollection<ProductDto>>.Failure("Отказано в доступе");
                }

                if (!long.TryParse(targetUserId, out var parsedUserId))
                {
                    return Result<ICollection<ProductDto>>.Failure("Некорректный формат ID пользователя");
                }
                resolvedUserId = parsedUserId;
            }
            else
            {
                resolvedUserId = currentUserId;
            }

            try
            {
                var favoriteProducts = await _appDbContext.FavoriteProducts
                    .AsNoTracking()
                    .Where(fp => fp.UserId == resolvedUserId)
                    .Select(fp => new ProductDto
                    {
                        Id = fp.Product.Id,
                        Name = fp.Product.Name,
                        Description = fp.Product.Description,
                        Price = fp.Product.Price,
                        PromotionalPrice = fp.Product.PromotionalPrice,
                        Size = fp.Product.Size,
                        StockQuantity = fp.Product.StockQuantity,
                        Owner = new UserBriefInfoDto
                        {
                            Id = fp.User.Id,
                            FirstName = fp.User.FirstName,
                            LastName = fp.User.LastName,
                            Email = fp.User.Email!,
                            PhoneNumber = fp.User.PhoneNumber
                        },
                        Currency = fp.Product.Currency,
                        IsActive = fp.Product.IsActive,
                        CreatedAt = fp.Product.CreatedAt,
                        UpdatedAt = fp.Product.UpdatedAt,
                        ProductImages = fp.Product.ProductImages
                            .OrderByDescending(pi => pi.IsMain)
                            .Select(pi => new ProductImageDto
                            {
                                Url = pi.Url,
                                IsMain = pi.IsMain
                            }).ToList(),
                        IsFavirite = true,
                        IsAdded = fp.User.Cart.CartItems.Any(ci => ci.ProductId == fp.ProductId)
                    })
                    .ToListAsync();

                return Result<ICollection<ProductDto>>.Success(favoriteProducts);
            }
            catch
            {
                throw;
            }
        }

        public async Task<Result<string>> RemoveFromFavoritesAsync(ClaimsPrincipal userPrincipal, string productId)
        {
            if (!long.TryParse(productId, out var id))
            {
                return Result<string>.Failure("Неверный формат ID товара");
            }

            // Получение текущего пользователя
            var userId = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out var currentUserId))
            {
                return Result<string>.Failure("Не удалось идентифицировать пользователя");
            }

            using var transaction = await _appDbContext.Database.BeginTransactionAsync();

            try
            {
                // Поиск закладки
                var favoriteProduct = _appDbContext.FavoriteProducts.FirstOrDefault(fp => fp.Id == id);

                if (favoriteProduct == null)
                {
                    return Result<string>.Failure("Товар не найден");
                }

                // Проверка прав доступа
                if (favoriteProduct.UserId != currentUserId)
                {
                    return Result<string>.Failure("Отказано в доступе");
                }

                _appDbContext.FavoriteProducts.Remove(favoriteProduct);
                await _appDbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return Result<string>.Success($"Продукт с ID {productId} успешно удален");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
