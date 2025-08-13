using marketplace_practice.Models;
using marketplace_practice.Models.Enums;
using marketplace_practice.Services.dto;
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

        public ProductService(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<Result<ProductDto>> CreateProductAsync(
            ClaimsPrincipal userPrincipal,
            string name,
            string? description,
            decimal price,
            Currency currency,
            string category,
            string? subcategory,
            ICollection<string>? imagesUrl)
        {
            // Получение пользователя из ClaimsPrincipal
            var userId = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out var ownerId))
            {
                return Result<ProductDto>.Failure("Не удалось идентифицировать пользователя");
            }

            // Начало транзакции
            using var transaction = await _appDbContext.Database.BeginTransactionAsync();

            try
            {
                // Поиск или создание группы (категории)
                var group = await _appDbContext.Groups
                    .Include(g => g.Category)
                    .Include(g => g.Subcategory)
                    .FirstOrDefaultAsync(g => g.Category.Name == category &&
                        (string.IsNullOrEmpty(subcategory)
                            ? g.Subcategory == null
                            : g.Subcategory.Name == subcategory));

                if (group == null)
                {
                    var categoryEntity = await _appDbContext.Categories
                        .FirstOrDefaultAsync(c => c.Name == category)
                        ?? new Category
                        {
                            Name = category,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            IsActive = true
                        };

                    Subcategory? subcategoryEntity = null;
                    if (!string.IsNullOrEmpty(subcategory))
                    {
                        subcategoryEntity = await _appDbContext.Subcategories
                            .FirstOrDefaultAsync(s => s.Name == subcategory)
                            ?? new Subcategory
                            {
                                Name = subcategory,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow,
                                IsActive = true
                            };
                    }

                    group = new Group
                    {
                        Category = categoryEntity,
                        Subcategory = subcategoryEntity
                    };

                    await _appDbContext.Groups.AddAsync(group);
                    await _appDbContext.SaveChangesAsync();
                }

                var product = new Product
                {
                    Name = name,
                    Description = description,
                    Price = price,
                    Currency = currency,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UserId = ownerId
                };

                // Добавление изображений
                ICollection<ProductImage> productImages = new List<ProductImage>();
                if (imagesUrl != null && imagesUrl.Any())
                {
                    for (int i = 0; i < imagesUrl.Count; i++)
                    {
                        productImages.Add(new ProductImage
                        {
                            Product = product,
                            Url = imagesUrl.ElementAt(i),
                            IsMain = i == 0, // Первое изображение - основное
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                    product.ProductImages = productImages;
                }

                // Добавление продукта в группу
                product.Groups.Add(group);

                await _appDbContext.Products.AddAsync(product);
                await _appDbContext.SaveChangesAsync();

                // Получение владельца для DTO
                var owner = await _appDbContext.Users
                    .Where(u => u.Id == ownerId)
                    .Select(u => new { u.UserName, u.FirstName, u.LastName })
                    .FirstOrDefaultAsync();

                await transaction.CommitAsync();

                return Result<ProductDto>.Success(new ProductDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    OwnerName = owner != null ? $"{owner.FirstName} {owner.LastName}" : owner?.UserName,
                    Currency = currency,
                    IsActive = product.IsActive,
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt,
                    Groups = new List<GroupDto>
                    {
                        new GroupDto
                        {
                            Category = category,
                            Subcategory = subcategory
                        }
                    },
                    ProductImages = productImages.Select(pi => new ProductImageDto
                    {
                        Url = pi.Url,
                        IsMain = pi.IsMain
                    }).ToList()
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Result<ProductDto>> GetProductByIdAsync(string productId)
        {
            if (!long.TryParse(productId, out var id))
            {
                return Result<ProductDto>.Failure("Неверный формат ID товара");
            }

            try
            {
                var product = await _appDbContext.Products
                    .AsNoTracking()
                    .Include(p => p.User)
                    .Include(p => p.Groups)
                        .ThenInclude(g => g.Category)
                    .Include(p => p.Groups)
                        .ThenInclude(g => g.Subcategory)
                    .Include(p => p.ProductImages)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                {
                    return Result<ProductDto>.Failure("Товар не найден");
                }

                var productDto = new ProductDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    Currency = product.Currency,
                    IsActive = product.IsActive,
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt,
                    OwnerName = product.User?.FirstName != null
                        ? $"{product.User.FirstName} {product.User.LastName}"
                        : product.User?.UserName,
                    Groups = product.Groups.Select(g => new GroupDto
                    {
                        Category = g.Category.Name,
                        Subcategory = g.Subcategory?.Name
                    }).ToList(),
                    ProductImages = product.ProductImages.Select(pi => new ProductImageDto
                    {
                        Url = pi.Url,
                        IsMain = pi.IsMain
                    }).OrderByDescending(pi => pi.IsMain).ToList()
                };

                return Result<ProductDto>.Success(productDto);
            }
            catch
            {
                throw;
            }
        }

        public async Task<Result<ProductDto>> UpdateProductAsync(
            string productId,
            string? name,
            string? description,
            decimal? price,
            Currency? currency,
            string? category,
            string? subcategory,
            ICollection<string>? imagesUrl)
        {
            if (!long.TryParse(productId, out var id))
            {
                return Result<ProductDto>.Failure("Неверный формат ID товара");
            }

            using var transaction = await _appDbContext.Database.BeginTransactionAsync();

            try
            {
                // Получение продукта со всеми связанными данными
                var product = await _appDbContext.Products
                    .Include(p => p.User)
                    .Include(p => p.Groups)
                        .ThenInclude(g => g.Category)
                    .Include(p => p.Groups)
                        .ThenInclude(g => g.Subcategory)
                    .Include(p => p.ProductImages)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                {
                    return Result<ProductDto>.Failure("Товар не найден");
                }

                // Обновление основных полей
                if (!string.IsNullOrEmpty(name)) product.Name = name;
                if (!string.IsNullOrEmpty(description)) product.Description = description;
                if (price.HasValue) product.Price = price.Value;
                if (currency.HasValue) product.Currency = currency.Value;
                product.UpdatedAt = DateTime.UtcNow;

                // Обновление категории/подкатегории
                if (category != null || subcategory != null)
                {
                    var currentGroup = product.Groups.FirstOrDefault();
                    var newCategory = category ?? currentGroup?.Category.Name;
                    var newSubcategory = subcategory ?? currentGroup?.Subcategory?.Name;

                    // Удаление старых связей с группами
                    product.Groups.Clear();

                    if (!string.IsNullOrEmpty(newCategory))
                    {
                        var group = await _appDbContext.Groups
                            .Include(g => g.Category)
                            .Include(g => g.Subcategory)
                            .FirstOrDefaultAsync(g => g.Category.Name == newCategory &&
                                (string.IsNullOrEmpty(newSubcategory)
                                    ? g.Subcategory == null
                                    : g.Subcategory.Name == newSubcategory));

                        if (group == null)
                        {
                            group = new Group
                            {
                                Category = await _appDbContext.Categories
                                    .FirstOrDefaultAsync(c => c.Name == newCategory)
                                    ?? new Category 
                                    { 
                                        Name = newCategory, 
                                        CreatedAt = DateTime.UtcNow,
                                        UpdatedAt = DateTime.UtcNow,
                                        IsActive = true
                                    },
                                Subcategory = string.IsNullOrEmpty(newSubcategory)
                                    ? null
                                    : await _appDbContext.Subcategories
                                        .FirstOrDefaultAsync(s => s.Name == newSubcategory)
                                        ?? new Subcategory
                                        {
                                            Name = newCategory,
                                            CreatedAt = DateTime.UtcNow,
                                            UpdatedAt = DateTime.UtcNow,
                                            IsActive = true
                                        }
                            };
                            await _appDbContext.Groups.AddAsync(group);
                        }

                        product.Groups.Add(group);
                    }
                }

                // Обновление изображения
                if (imagesUrl != null)
                {
                    // Удаление старых изображений
                    _appDbContext.ProductImages.RemoveRange(product.ProductImages);

                    // Добавление новых
                    var productImages = imagesUrl.Select((url, index) => new ProductImage
                    {
                        ProductId = product.Id,
                        Url = url,
                        IsMain = index == 0,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }).ToList();

                    await _appDbContext.ProductImages.AddRangeAsync(productImages);
                    product.ProductImages = productImages;
                }

                await _appDbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                // Создание DTO
                var productDto = new ProductDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    Currency = product.Currency,
                    IsActive = product.IsActive,
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt,
                    OwnerName = product.User?.FirstName != null
                        ? $"{product.User.FirstName} {product.User.LastName}"
                        : product.User?.UserName,
                    Groups = product.Groups.Select(g => new GroupDto
                    {
                        Category = g.Category.Name,
                        Subcategory = g.Subcategory?.Name
                    }).ToList(),
                    ProductImages = product.ProductImages.Select(pi => new ProductImageDto
                    {
                        Url = pi.Url,
                        IsMain = pi.IsMain
                    }).OrderByDescending(pi => pi.IsMain).ToList()
                };

                return Result<ProductDto>.Success(productDto);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Result<string>> DeleteProductAsync(string productId)
        {
            if (!long.TryParse(productId, out var id))
            {
                return Result<string>.Failure("Неверный формат ID товара");
            }

            using var transaction = await _appDbContext.Database.BeginTransactionAsync();

            try
            {
                // Получение продукта со всеми зависимостями, которые нужно удалить
                var product = await _appDbContext.Products
                    .Include(p => p.ProductImages)
                    .Include(p => p.OrderItems)
                    .Include(p => p.FavoriteProducts)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                {
                    return Result<string>.Failure("Товар не найден");
                }

                _appDbContext.Products.Remove(product);
                await _appDbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return Result<string>.Success($"Продукт с ID {productId} успешно удален");
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
