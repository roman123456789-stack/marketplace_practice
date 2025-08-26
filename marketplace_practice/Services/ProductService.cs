using marketplace_practice.Controllers.dto.Products;
using marketplace_practice.Models;
using marketplace_practice.Models.Enums;
using marketplace_practice.Services.dto.Products;
using marketplace_practice.Services.dto.Users;
using marketplace_practice.Services.interfaces;
using marketplace_practice.Services.service_models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Security.Claims;

namespace marketplace_practice.Services
{
    public class ProductService : IProductService
    {
        private readonly AppDbContext _appDbContext;
        private readonly IFileUploadService _fileUploadService;
        private readonly IFavoriteCookieService _favoriteCookieService;
        private readonly ICartCookieService _cartCookieService;

        public ProductService(
            AppDbContext appDbContext, 
            IFileUploadService fileUploadService,
            IFavoriteCookieService favoriteCookieService,
            ICartCookieService cartCookieService)
        {
            _appDbContext = appDbContext;
            _fileUploadService = fileUploadService;
            _favoriteCookieService = favoriteCookieService;
            _cartCookieService = cartCookieService;
        }

        public async Task<Result<ProductDto>> CreateProductAsync(
            ClaimsPrincipal userPrincipal,
            string name,
            string? description,
            decimal price,
            decimal? promotionalPrice,
            short? size,
            string currency,
            ICollection<CategoryHierarchyDto> categoryHierarchies,
            List<IFormFile>? images,
            int stockQuantity = 0)
        {
            // Получение пользователя
            var userId = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out var ownerId))
            {
                return Result<ProductDto>.Failure("Не удалось идентифицировать пользователя");
            }

            using var transaction = await _appDbContext.Database.BeginTransactionAsync();

            try
            {
                // Создание продукта
                var product = new Product
                {
                    Name = name,
                    Description = description,
                    Price = price,
                    PromotionalPrice = promotionalPrice,
                    Size = size,
                    StockQuantity = stockQuantity,
                    Currency = currency.ParseFromDisplayName<Currency>(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UserId = ownerId
                };

                // Обработка категорий
                var processedCategories = new List<Category>();
                foreach (var hierarchy in categoryHierarchies)
                {
                    var currentLevel = hierarchy;
                    Category? parentCategory = null;
                    Category? targetCategory = null;

                    // Проход по всей иерархии, чтобы найти конечную категорию
                    while (currentLevel != null)
                    {
                        // Поиск существующей категории
                        var category = await _appDbContext.Categories
                            .FirstOrDefaultAsync(c => c.Name == currentLevel.Name &&
                                                   c.ParentCategoryId == (parentCategory != null ? parentCategory.Id : (short?)null) &&
                                                   c.IsActive);

                        if (category == null)
                        {
                            // Категория не найдена
                            await transaction.RollbackAsync();
                            return Result<ProductDto>.Failure($"Категория '{currentLevel.Name}' не найдена" +
                                (parentCategory != null ? $" в категории '{parentCategory.Name}'" : ""));
                        }

                        parentCategory = category;
                        targetCategory = category;
                        currentLevel = currentLevel.Child;

                        // Если это последний уровень в иерархии - выход
                        if (currentLevel == null)
                        {
                            break;
                        }
                    }

                    // Добавление конечной категории к продукту
                    if (targetCategory != null)
                    {
                        // Проверка, что это действительно конечная категория
                        var hasSubcategories = await _appDbContext.Categories
                            .AnyAsync(c => c.ParentCategoryId == targetCategory.Id && c.IsActive);

                        if (hasSubcategories)
                        {
                            await transaction.RollbackAsync();
                            return Result<ProductDto>.Failure($"Категория '{targetCategory.Name}' не является конечной. Укажите конкретную подкатегорию.");
                        }

                        product.Categories.Add(targetCategory);
                        processedCategories.Add(targetCategory);
                    }
                }

                // Проверка, что товар добавлен хотя бы в одну категорию
                if (!product.Categories.Any())
                {
                    await transaction.RollbackAsync();
                    return Result<ProductDto>.Failure("Товар не был добавлен ни в одну из категорий");
                }

                // Обработка изображений
                if (images != null)
                {
                    if (images.Count > 10)
                        return Result<ProductDto>.Failure("Может быть загружено не более 10 изображений");

                    var urls = await _fileUploadService.SaveFilesAsync(images, "products");

                    if (urls != null && urls.Any())
                    {
                        product.ProductImages = urls.Select((url, index) => new ProductImage
                        {
                            Url = url,
                            IsMain = index == 0,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        }).ToList();
                    }
                }

                await _appDbContext.Products.AddAsync(product);
                await _appDbContext.SaveChangesAsync();

                // Получение информации о владельце
                var owner = await _appDbContext.Users
                    .Where(u => u.Id == ownerId)
                    .Select(u => new UserBriefInfoDto
                    {
                        Id = u.Id,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Email = u.Email!,
                        PhoneNumber = u.PhoneNumber
                    })
                    .FirstOrDefaultAsync();

                await transaction.CommitAsync();

                return Result<ProductDto>.Success(new ProductDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    PromotionalPrice = product.PromotionalPrice,
                    Size = product.Size,
                    StockQuantity = product.StockQuantity,
                    Owner = owner!,
                    Currency = currency,
                    IsActive = product.IsActive,
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt,
                    ProductImages = product.ProductImages?.Select(pi => new ProductImageDto
                    {
                        Url = pi.Url,
                        IsMain = pi.IsMain
                    }).ToList(),
                    IsFavirite = false,
                    IsAdded = false
                });
            }
            catch (ArgumentException ex)
            {
                await transaction.RollbackAsync();
                return Result<ProductDto>.Failure(ex.Message);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Result<ProductDto>> GetProductByIdAsync(
            ClaimsPrincipal userPrincipal, 
            string productId, 
            HttpContext httpContext)
        {
            // Валидация ID товара
            if (!long.TryParse(productId, out var id))
            {
                return Result<ProductDto>.Failure("Неверный формат ID товара");
            }

            try
            {
                // Получаем информацию о товаре
                var productDto = await _appDbContext.Products
                    .AsNoTracking()
                    .Where(p => p.Id == id && p.IsActive)
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
                            }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (productDto == null)
                {
                    return Result<ProductDto>.Failure("Товар не найден");
                }

                // Установка флагов в зависимости от авторизации
                if (userPrincipal.Identity.IsAuthenticated)
                {
                    // Для авторизованных - из БД
                    var userId = long.Parse(userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier));
                    productDto.IsFavirite = await _appDbContext.FavoriteProducts
                        .AnyAsync(fp => fp.UserId == userId && fp.ProductId == id);
                    productDto.IsAdded = await _appDbContext.CartItems
                        .AnyAsync(ci => ci.Cart.UserId == userId && ci.ProductId == id);
                }
                else
                {
                    // Для неавторизованных - из куки
                    var favoriteIds = (await _favoriteCookieService.GetFavoritesFromCookieAsync(httpContext)).Value;
                    var cartItems = (await _cartCookieService.GetCartFromCookieAsync(httpContext)).Value;

                    productDto.IsFavirite = favoriteIds?.Contains(productId) == true;
                    productDto.IsAdded = cartItems?.Any(ci => ci.ProductId.ToString() == productId) == true;
                }

                return Result<ProductDto>.Success(productDto);
            }
            catch
            {
                throw;
            }
        }

        public async Task<Result<ICollection<ProductDto>>> GetProductListAsync(
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
                // Оптимизированный запрос с проекцией в DTO
                var productDtos = await _appDbContext.Products
                    .AsNoTracking()
                    .Where(p => p.UserId == resolvedUserId && p.IsActive)
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
                        IsFavirite = p.FavoriteProducts.Any(fp => fp.UserId == currentUserId),
                        IsAdded = p.CartItems.Any(ci => ci.Cart.UserId == currentUserId)
                    })
                    .ToListAsync();

                return Result<ICollection<ProductDto>>.Success(productDtos);
            }
            catch
            {
                throw;
            }
        }

        public async Task<Result<ProductDto>> UpdateProductAsync(
            ClaimsPrincipal userPrincipal,
            string productId,
            string? name,
            string? description,
            decimal? price,
            decimal? promotionalPrice,
            short? size,
            string? currency,
            ICollection<CategoryHierarchyDto>? categoryHierarchies,
            ICollection<string>? imagesUrl,
            int? stockQuantity)
        {
            // Валидация ID товара
            if (!long.TryParse(productId, out var id))
            {
                return Result<ProductDto>.Failure("Неверный формат ID товара");
            }

            // Получение текущего пользователя
            var userId = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out var currentUserId))
            {
                return Result<ProductDto>.Failure("Не удалось идентифицировать пользователя");
            }

            using var transaction = await _appDbContext.Database.BeginTransactionAsync();

            try
            {
                // Проверка прав доступа и получение продукта
                var product = await _appDbContext.Products
                    .Include(p => p.User)
                    .Include(p => p.Categories)
                    .Include(p => p.ProductImages)
                    .Include(p => p.FavoriteProducts)
                    .Include(p => p.CartItems)
                        .ThenInclude(ci => ci.Cart)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                {
                    return Result<ProductDto>.Failure("Товар не найден");
                }

                if (product.UserId != currentUserId)
                {
                    return Result<ProductDto>.Failure("Отказано в доступе");
                }

                // Обновление основных полей
                if (!string.IsNullOrEmpty(name)) product.Name = name;
                if (description != null) product.Description = description;
                if (price.HasValue) product.Price = price.Value;
                if (!string.IsNullOrEmpty(currency)) product.Currency = currency.ParseFromDisplayName<Currency>();
                if (promotionalPrice.HasValue) product.PromotionalPrice = promotionalPrice;
                if (size.HasValue) product.Size = size.Value;
                if (stockQuantity.HasValue) product.StockQuantity = stockQuantity.Value;
                product.UpdatedAt = DateTime.UtcNow;

                // Обновление категорий (если указаны)
                if (categoryHierarchies != null)
                {
                    await UpdateProductCategoriesAsync(product, categoryHierarchies);
                }

                // Обновление изображений (если указаны)
                if (imagesUrl != null)
                {
                    await UpdateProductImagesAsync(product.Id, imagesUrl);
                }

                await _appDbContext.SaveChangesAsync();

                await transaction.CommitAsync();

                return Result<ProductDto>.Success(new ProductDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    PromotionalPrice = product.PromotionalPrice,
                    Size = product.Size,
                    StockQuantity = product.StockQuantity,
                    Currency = product.Currency.GetDisplayName(),
                    IsActive = product.IsActive,
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt,
                    Owner = new UserBriefInfoDto
                    {
                        Id = product.User.Id,
                        FirstName = product.User.FirstName,
                        LastName = product.User.LastName,
                        Email = product.User.Email!,
                        PhoneNumber = product.User.PhoneNumber
                    },
                    ProductImages = product.ProductImages
                        .OrderByDescending(pi => pi.IsMain)
                        .Select(pi => new ProductImageDto
                        {
                            Url = pi.Url,
                            IsMain = pi.IsMain
                        }).ToList(),
                    IsFavirite = product.FavoriteProducts.Any(fp => fp.UserId == currentUserId),
                    IsAdded = product.CartItems.Any(ci => ci.Cart.UserId == currentUserId)
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Result<ProductDto>.Failure($"Ошибка при обновлении товара: {ex.Message}");
            }
        }

        private async Task<Result<string>> UpdateProductCategoriesAsync(
            Product product,
            ICollection<CategoryHierarchyDto> categoryHierarchies)
        {
            try
            {
                product.Categories.Clear();

                if (categoryHierarchies == null || !categoryHierarchies.Any())
                {
                    return Result<string>.Failure("Не указаны категории для товара");
                }

                var processedCategories = new List<Category>();

                foreach (var hierarchy in categoryHierarchies)
                {
                    var currentLevel = hierarchy;
                    Category? parentCategory = null;
                    Category? targetCategory = null;

                    while (currentLevel != null)
                    {
                        var category = await _appDbContext.Categories
                            .FirstOrDefaultAsync(c => c.Name == currentLevel.Name &&
                                                   c.ParentCategoryId == (parentCategory != null ? parentCategory.Id : (short?)null) &&
                                                   c.IsActive);

                        if (category == null)
                        {
                            return Result<string>.Failure($"Категория '{currentLevel.Name}' не найдена" +
                                (parentCategory != null ? $" в категории '{parentCategory.Name}'" : ""));
                        }

                        parentCategory = category;
                        targetCategory = category;
                        currentLevel = currentLevel.Child;
                    }

                    // Проверка, что это конечная категория
                    if (targetCategory != null)
                    {
                        var hasSubcategories = await _appDbContext.Categories
                            .AnyAsync(c => c.ParentCategoryId == targetCategory.Id && c.IsActive);

                        if (hasSubcategories)
                        {
                            return Result<string>.Failure($"Категория '{targetCategory.Name}' не является конечной. Укажите конкретную подкатегорию.");
                        }

                        if (!processedCategories.Any(c => c.Id == targetCategory.Id))
                        {
                            product.Categories.Add(targetCategory);
                            processedCategories.Add(targetCategory);
                        }
                    }
                }

                if (!product.Categories.Any())
                {
                    return Result<string>.Failure("Товар не был добавлен ни в одну из категорий");
                }

                return Result<string>.Success(string.Empty);
            }
            catch (Exception ex)
            {
                return Result<string>.Failure($"Ошибка при обновлении категорий товара: {ex.Message}");
            }
        }

        private async Task UpdateProductImagesAsync(long productId, ICollection<string> imagesUrl)
        {
            // Удаление старых изображений
            await _appDbContext.ProductImages
                .Where(pi => pi.ProductId == productId)
                .ExecuteDeleteAsync();

            // Добавление новых изображений
            if (imagesUrl.Any())
            {
                var productImages = imagesUrl
                    .Select((url, index) => new ProductImage
                    {
                        ProductId = productId,
                        Url = url,
                        IsMain = index == 0,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    })
                    .ToList();

                await _appDbContext.ProductImages.AddRangeAsync(productImages);
            }
        }

        public async Task<Result<string>> DeleteProductAsync(ClaimsPrincipal userPrincipal, string productId)
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
                // Получение продукта со всеми зависимостями, которые нужно удалить
                var product = await _appDbContext.Products
                    .Include(p => p.ProductImages)
                    .Include(p => p.CartItems)
                    .Include(p => p.FavoriteProducts)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                {
                    return Result<string>.Failure("Товар не найден");
                }

                // Проверка прав доступа
                if (product.UserId != currentUserId)
                {
                    return Result<string>.Failure("Отказано в доступе");
                }

                _appDbContext.Products.Remove(product);
                await _appDbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return Result<string>.Success(string.Empty);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23503")
            {
                await transaction.RollbackAsync();
                return Result<string>.Failure("Нельзя удалить товар, так как он используется в других записях");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
