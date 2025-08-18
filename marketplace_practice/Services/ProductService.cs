using marketplace_practice.Models;
using marketplace_practice.Models.Enums;
using marketplace_practice.Services.dto.Orders;
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
                        : g.Subcategory != null && g.Subcategory.Name == subcategory));

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
                    .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email, u.PhoneNumber })
                    .FirstOrDefaultAsync();

                await transaction.CommitAsync();

                return Result<ProductDto>.Success(new ProductDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    Owner = new UserBriefInfoDto
                    {
                        Id = owner!.Id,
                        FirstName = owner.FirstName,
                        LastName = owner.LastName,
                        Email = owner.Email!,
                        PhoneNumber = owner.PhoneNumber,
                    },
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
                    }).ToList(),
                    IsFavirite = false,
                    IsAdded = false
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Result<ProductDto>> GetProductByIdAsync(ClaimsPrincipal userPrincipal, string productId)
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

            try
            {
                // Оптимизированный запрос с проекцией в DTO
                var productDto = await _appDbContext.Products
                    .AsNoTracking()
                    .Where(p => p.Id == id)
                    .Select(p => new ProductDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        Currency = p.Currency,
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
                        Groups = p.Groups.Select(g => new GroupDto
                        {
                            Category = g.Category.Name,
                            Subcategory = g.Subcategory != null ? g.Subcategory.Name : null
                        }).ToList(),
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
                    .FirstOrDefaultAsync();

                if (productDto == null)
                {
                    return Result<ProductDto>.Failure("Товар не найден");
                }

                // Проверка прав доступа
                if (productDto.Owner.Id != currentUserId && !userPrincipal.IsInRole("Admin"))
                {
                    return Result<ProductDto>.Failure("Отказано в доступе");
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
            // Получение пользователя из ClaimsPrincipal
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
                if (userId != targetUserId && !userPrincipal.IsInRole("Admin"))
                {
                    return Result<ICollection<ProductDto>>.Failure("Отказано в доступе");
                }

                // Преобразование строки в long
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
                var productList = await _appDbContext.Products
                    .AsNoTracking()
                    .Where(p => p.UserId == resolvedUserId)
                    .OrderByDescending(ci => ci.CreatedAt)
                    .Select(p => new ProductDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        Currency = p.Currency,
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
                        Groups = p.Groups.Select(g => new GroupDto
                        {
                            Category = g.Category.Name,
                            Subcategory = g.Subcategory != null ? g.Subcategory.Name : null
                        }).ToList(),
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

                return Result<ICollection<ProductDto>>.Success(productList);
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
            Currency? currency,
            string? category,
            string? subcategory,
            ICollection<string>? imagesUrl)
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
                // Проверка прав доступа и получение базовой информации о продукте
                var productInfo = await _appDbContext.Products
                    .Where(p => p.Id == id)
                    .Select(p => new
                    {
                        Product = p,
                        HasAccess = p.UserId == currentUserId || userPrincipal.IsInRole("Admin")
                    })
                    .FirstOrDefaultAsync();

                if (productInfo == null)
                {
                    return Result<ProductDto>.Failure("Товар не найден");
                }

                if (!productInfo.HasAccess)
                {
                    return Result<ProductDto>.Failure("Отказано в доступе");
                }

                var product = productInfo.Product;

                // Обновление основных полей
                if (!string.IsNullOrEmpty(name)) product.Name = name;
                if (description != null) product.Description = description;
                if (price.HasValue) product.Price = price.Value;
                if (currency.HasValue) product.Currency = currency.Value;
                product.UpdatedAt = DateTime.UtcNow;

                // Обновление категории/подкатегории (только если указаны)
                if (category != null || subcategory != null)
                {
                    await UpdateProductGroupsAsync(product, category, subcategory);
                }

                // Обновление изображений (только если указаны)
                if (imagesUrl != null)
                {
                    await UpdateProductImagesAsync(product.Id, imagesUrl);
                }

                await _appDbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                // Получение обновленного продукта с проекцией в DTO
                var productDto = await _appDbContext.Products
                    .Where(p => p.Id == id)
                    .Select(p => new ProductDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        Currency = p.Currency,
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
                        Groups = p.Groups.Select(g => new GroupDto
                        {
                            Category = g.Category.Name,
                            Subcategory = g.Subcategory != null ? g.Subcategory.Name : null
                        }).ToList(),
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
                    .FirstAsync();

                return Result<ProductDto>.Success(productDto);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task UpdateProductGroupsAsync(Product product, string? category, string? subcategory)
        {
            // Удаление старых связей
            product.Groups.Clear();

            if (string.IsNullOrEmpty(category))
                return;

            // Поиск или создание группы
            var group = await _appDbContext.Groups
                .Include(g => g.Category)
                .Include(g => g.Subcategory)
                .FirstOrDefaultAsync(g => g.Category.Name == category &&
                    (string.IsNullOrEmpty(subcategory)
                        ? g.Subcategory == null
                        : g.Subcategory != null && g.Subcategory.Name == subcategory));

            if (group == null)
            {
                var categoryEntity = await _appDbContext.Categories
                    .FirstOrDefaultAsync(c => c.Name == category)
                    ?? new Category { Name = category };

                Subcategory? subcategoryEntity = null;
                if (!string.IsNullOrEmpty(subcategory))
                {
                    subcategoryEntity = await _appDbContext.Subcategories
                        .FirstOrDefaultAsync(s => s.Name == subcategory)
                        ?? new Subcategory { Name = subcategory };
                }

                group = new Group
                {
                    Category = categoryEntity,
                    Subcategory = subcategoryEntity
                };
                _appDbContext.Groups.Add(group);
            }

            product.Groups.Add(group);
        }

        private async Task UpdateProductImagesAsync(long productId, ICollection<string> imagesUrl)
        {
            // Удаление старых изображений
            await _appDbContext.ProductImages
                .Where(pi => pi.ProductId == productId)
                .ExecuteDeleteAsync();

            // Добавление новых изображений
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
                    .Include(p => p.OrderItems)
                    .Include(p => p.FavoriteProducts)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                {
                    return Result<string>.Failure("Товар не найден");
                }

                // Проверка прав доступа
                if (product.UserId != currentUserId && !userPrincipal.IsInRole("Admin"))
                {
                    return Result<string>.Failure("Отказано в доступе");
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
