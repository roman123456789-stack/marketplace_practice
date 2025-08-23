using marketplace_practice.Models.Enums;
using marketplace_practice.Services.dto.Products;
using marketplace_practice.Services.dto.Users;
using marketplace_practice.Services.interfaces;
using marketplace_practice.Services.service_models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace marketplace_practice.Services
{
    public class FeaturedProductsService : IFeaturedProductsService
    {
        private readonly AppDbContext _appDbContext;

        public FeaturedProductsService(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<Result<ICollection<ProductDto>>> GetPopularProductsAsync(
            ClaimsPrincipal userPrincipal,
            int limit = 4)
        {
            long? currentUserId = null;

            // Получение ID пользователя только если он авторизован
            var userId = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId) && long.TryParse(userId, out var parsedUserId))
            {
                currentUserId = parsedUserId;
            }

            try
            {
                // Получение списка популярных товаров
                var popularProducts = await _appDbContext.Products
                    .AsNoTracking()
                    .Where(p => p.IsActive)
                    .Select(p => new
                    {
                        Product = p,
                        CartItemsCount = p.CartItems.Count,
                        FavoriteCount = p.FavoriteProducts.Count,
                        OrderCount = p.CartItems
                            .Where(ci => ci.OrderItem != null)
                            .Select(ci => ci.OrderItem.OrderId)
                            .Distinct()
                            .Count(),
                        RecentSales = p.CartItems
                            .Where(ci => ci.OrderItem != null &&
                                        ci.OrderItem.Order != null &&
                                        ci.OrderItem.Order.CreatedAt >= DateTime.UtcNow.AddDays(-30) &&
                                        ci.OrderItem.Order.Status >= OrderStatus.Completed)
                            .Sum(ci => ci.OrderItem.Quantity)
                    })
                    .OrderByDescending(x => x.CartItemsCount)      // Основной критерий - в корзинах
                    .ThenByDescending(x => x.FavoriteCount)        // Второй критерий - в избранном
                    .ThenByDescending(x => x.OrderCount)           // Третий критерий - купленные
                    .ThenByDescending(x => x.RecentSales)          // Четвертый критерий - недавние продажи
                    .ThenByDescending(x => x.Product.CreatedAt)    // Пятый критерий - новизна
                    .Take(limit)
                    .Select(x => new ProductDto
                    {
                        Id = x.Product.Id,
                        Name = x.Product.Name,
                        Description = x.Product.Description,
                        Price = x.Product.Price,
                        PromotionalPrice = x.Product.PromotionalPrice,
                        Size = x.Product.Size,
                        StockQuantity = x.Product.StockQuantity,
                        Currency = x.Product.Currency.GetDisplayName(),
                        IsActive = x.Product.IsActive,
                        CreatedAt = x.Product.CreatedAt,
                        UpdatedAt = x.Product.UpdatedAt,
                        Owner = new UserBriefInfoDto
                        {
                            Id = x.Product.User.Id,
                            FirstName = x.Product.User.FirstName,
                            LastName = x.Product.User.LastName,
                            Email = x.Product.User.Email!,
                            PhoneNumber = x.Product.User.PhoneNumber
                        },
                        ProductImages = x.Product.ProductImages
                            .OrderByDescending(pi => pi.IsMain)
                            .Select(pi => new ProductImageDto
                            {
                                Url = pi.Url,
                                IsMain = pi.IsMain
                            })
                            .ToList(),
                        IsFavirite = currentUserId.HasValue
                            ? x.Product.FavoriteProducts.Any(fp => fp.UserId == currentUserId.Value)
                            : false,
                        IsAdded = currentUserId.HasValue
                            ? x.Product.CartItems.Any(ci => ci.Cart.UserId == currentUserId.Value)
                            : false
                    })
                    .ToListAsync();

                return Result<ICollection<ProductDto>>.Success(popularProducts);
            }
            catch
            {
                throw;
            }
        }

        public async Task<Result<ICollection<ProductDto>>> GetNewProductsAsync(
            ClaimsPrincipal userPrincipal,
            int limit = 4,
            int days = 30)
        {
            long? currentUserId = null;

            // Получение ID пользователя только если он авторизован
            var userId = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId) && long.TryParse(userId, out var parsedUserId))
            {
                currentUserId = parsedUserId;
            }

            try
            {
                var sinceDate = DateTime.UtcNow.AddDays(-days);

                // Получение списка новых товаров
                var newProducts = await _appDbContext.Products
                    .AsNoTracking()
                    .Where(p => p.IsActive && p.CreatedAt >= sinceDate)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(limit)
                    .Select(p => new ProductDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        PromotionalPrice = p.PromotionalPrice,
                        Size = p.Size,
                        StockQuantity = p.StockQuantity,
                        Currency = p.Currency.GetDisplayName(),
                        IsActive = p.IsActive,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                        Owner = new UserBriefInfoDto
                        {
                            Id = p.User.Id,
                            FirstName = p.User.FirstName,
                            LastName = p.User.LastName,
                            Email = p.User.Email!,
                            PhoneNumber = p.User.PhoneNumber
                        },
                        ProductImages = p.ProductImages
                        .OrderByDescending(pi => pi.IsMain)
                        .Select(pi => new ProductImageDto
                        {
                            Url = pi.Url,
                            IsMain = pi.IsMain
                        }).ToList(),
                        IsFavirite = currentUserId.HasValue
                            ? p.FavoriteProducts.Any(fp => fp.UserId == currentUserId.Value)
                            : false,
                        IsAdded = currentUserId.HasValue
                            ? p.CartItems.Any(ci => ci.Cart.UserId == currentUserId.Value)
                            : false
                    })
                    .ToListAsync();

                return Result<ICollection<ProductDto>>.Success(newProducts);
            }
            catch
            {
                throw;
            }
        }
    }
}
