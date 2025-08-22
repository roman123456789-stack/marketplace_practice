using marketplace_practice.Controllers.dto.Products;
using marketplace_practice.Models;
using marketplace_practice.Models.Enums;
using marketplace_practice.Services.dto.Catalog;
using marketplace_practice.Services.dto.Products;
using marketplace_practice.Services.dto.Users;
using marketplace_practice.Services.interfaces;
using marketplace_practice.Services.service_models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace marketplace_practice.Services
{
    public class CatalogService : ICatalogService
    {
        private readonly AppDbContext _appDbContext;

        public CatalogService(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<Result<CategoryHierarchyDto>> AddCategoryAsync(CategoryHierarchyDto categoryHierarchy)
        {
            using var transaction = await _appDbContext.Database.BeginTransactionAsync();

            try
            {
                if (categoryHierarchy == null)
                {
                    return Result<CategoryHierarchyDto>.Failure("Иерархия категорий не может быть пустой");
                }

                Category? parentCategory = null;
                Category? currentCategory = null;
                var currentHierarchy = categoryHierarchy;

                // Проходим по всей иерархии и создаем/находим категории
                while (currentHierarchy != null)
                {
                    // Проверяем, существует ли уже категория с таким именем на этом уровне
                    var existingCategory = await _appDbContext.Categories
                        .FirstOrDefaultAsync(c => c.Name.ToLower() == currentHierarchy.Name.ToLower() &&
                                                c.ParentCategoryId == (parentCategory != null ? parentCategory.Id : (short?)null));

                    if (existingCategory != null)
                    {
                        // Категория уже существует, переходим к следующему уровню
                        currentCategory = existingCategory;
                    }
                    else
                    {
                        // Создаем новую категорию
                        currentCategory = new Category
                        {
                            Name = currentHierarchy.Name.Trim(),
                            ParentCategoryId = parentCategory?.Id,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            IsActive = true
                        };

                        await _appDbContext.Categories.AddAsync(currentCategory);
                        await _appDbContext.SaveChangesAsync(); // Сохраняем чтобы получить ID
                    }

                    // Переходим к следующему уровню иерархии
                    parentCategory = currentCategory;
                    currentHierarchy = currentHierarchy.Child;
                }

                if (currentCategory == null)
                {
                    return Result<CategoryHierarchyDto>.Failure("Не удалось создать категорию");
                }

                await _appDbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                // Возвращаем полную иерархию созданной категории
                var resultHierarchy = await BuildCategoryHierarchyFromDbAsync(currentCategory.Id);

                return resultHierarchy != null
                    ? Result<CategoryHierarchyDto>.Success(resultHierarchy)
                    : Result<CategoryHierarchyDto>.Failure("Категория не найдена");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Вспомогательный метод для построения иерархии из БД
        private async Task<CategoryHierarchyDto?> BuildCategoryHierarchyFromDbAsync(short categoryId)
        {
            var category = await _appDbContext.Categories
                .Include(c => c.ParentCategory)
                .FirstOrDefaultAsync(c => c.Id == categoryId);

            if (category == null)
                return null;

            // Собираем всех родителей
            var categories = new List<Category>();
            var current = category;

            while (current != null)
            {
                categories.Add(current);
                current = current.ParentCategory;
            }

            // Разворачиваем для порядка от корня
            categories.Reverse();

            // Строим DTO иерархии
            CategoryHierarchyDto? root = null;
            CategoryHierarchyDto? currentDto = null;

            foreach (var cat in categories)
            {
                if (root == null)
                {
                    root = new CategoryHierarchyDto
                    {
                        Name = cat.Name,
                    };
                    currentDto = root;
                }
                else
                {
                    currentDto!.Child = new CategoryHierarchyDto
                    {
                        Name = cat.Name,
                    };
                    currentDto = currentDto.Child;
                }
            }

            return root;
        }

        public async Task<Result<ICollection<CategoryHierarchiesDto>>> GetCategoryHierarchiesAsync()
        {
            try
            {
                // Загружаем все категории одним запросом
                var allCategories = await _appDbContext.Categories
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                // Группируем по родительским ID с обработкой null
                var categoriesByParentId = allCategories
                    .GroupBy(c => c.ParentCategoryId ?? -1) // Используем -1 для null
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Получаем корневые категории (используем -1 для null)
                var rootCategories = categoriesByParentId.ContainsKey(-1)
                    ? categoriesByParentId[-1]
                    : new List<Category>();

                var hierarchies = new List<CategoryHierarchiesDto>();

                foreach (var rootCategory in rootCategories)
                {
                    var hierarchy = BuildCategoryHierarchy(rootCategory, categoriesByParentId);
                    hierarchies.Add(hierarchy);
                }

                return Result<ICollection<CategoryHierarchiesDto>>.Success(hierarchies);
            }
            catch
            {
                throw;
            }
        }

        // Рекурсивное построение иерархии из словаря
        private CategoryHierarchiesDto BuildCategoryHierarchy(Category category, Dictionary<short, List<Category>> categoriesByParentId)
        {
            var hierarchy = new CategoryHierarchiesDto
            {
                Name = category.Name,
                Children = new List<CategoryHierarchiesDto>()
            };

            if (categoriesByParentId.ContainsKey(category.Id))
            {
                foreach (var childCategory in categoriesByParentId[category.Id])
                {
                    var childHierarchy = new CategoryHierarchiesDto
                    {
                        Name = childCategory.Name,
                        Children = BuildChildHierarchy(childCategory, categoriesByParentId)
                    };

                    hierarchy.Children.Add(childHierarchy);
                }
            }

            return hierarchy;
        }

        // Рекурсивное построение дочерней иерархии
        private List<CategoryHierarchiesDto>? BuildChildHierarchy(Category category, Dictionary<short, List<Category>> categoriesByParentId)
        {
            if (!categoriesByParentId.ContainsKey(category.Id))
                return null;

            var children = new List<CategoryHierarchiesDto>();

            foreach (var childCategory in categoriesByParentId[category.Id])
            {
                var childHierarchy = new CategoryHierarchiesDto
                {
                    Name = childCategory.Name,
                    Children = BuildChildHierarchy(childCategory, categoriesByParentId)
                };

                children.Add(childHierarchy);
            }

            return children;
        }

        public async Task<Result<ICollection<ProductDto>>> GetProductsFromCategory(
            ClaimsPrincipal userPrincipal,
            string[] pathSegments)
        {
            // Валидация входных параметров
            if (pathSegments == null || pathSegments.Length == 0)
            {
                return Result<ICollection<ProductDto>>.Failure("Не указан путь категории");
            }

            long? currentUserId = null;

            // Получение ID пользователя только если он авторизован
            var userId = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId) && long.TryParse(userId, out var parsedUserId))
            {
                currentUserId = parsedUserId;
            }

            try
            {
                // Поиск категории по пути
                var category = await FindCategoryByPathAsync(pathSegments);
                if (category == null)
                {
                    return Result<ICollection<ProductDto>>.Failure("Категория не найдена");
                }

                // Сбор всех ID категорий (основная + все подкатегории)
                var allCategoryIds = new List<short> { category.Id };
                await CollectSubcategoryIdsAsync(category, allCategoryIds);

                var products = await _appDbContext.Products
                    .AsNoTracking()
                    .Where(p => p.IsActive &&
                               p.Categories.Any(c => allCategoryIds.Contains(c.Id)))
                    .Select(p => new ProductDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        PromotionalPrice = p.PromotionalPrice,
                        Size = p.Size,
                        StockQuantity = p.StockQuantity,
                        Owner = new UserBriefInfoDto
                        {
                            Id = p.User.Id,
                            FirstName = p.User.FirstName,
                            LastName = p.User.LastName,
                            Email = p.User.Email!,
                            PhoneNumber = p.User.PhoneNumber
                        },
                        Currency = p.Currency.GetDisplayName(),
                        IsActive = p.IsActive,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
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

                return Result<ICollection<ProductDto>>.Success(products);
            }
            catch
            {
                throw;
            }
        }

        // Вспомогательный метод для поиска категории по пути
        private async Task<Category?> FindCategoryByPathAsync(string[] pathSegments)
        {
            Category? currentCategory = null;

            foreach (var segment in pathSegments)
            {
                var categoryName = segment.ToLower().Trim();

                var query = _appDbContext.Categories
                    .Where(c => c.Name.ToLower() == categoryName && c.IsActive);

                if (currentCategory == null)
                {
                    // Ищем корневую категорию
                    query = query.Where(c => c.ParentCategoryId == null);
                }
                else
                {
                    // Ищем подкатегорию
                    query = query.Where(c => c.ParentCategoryId == currentCategory.Id);
                }

                currentCategory = await query.FirstOrDefaultAsync();

                if (currentCategory == null)
                    return null;
            }

            return currentCategory;
        }

        // Вспомогательный метод для рекурсивного сбора ID подкатегорий
        private async Task CollectSubcategoryIdsAsync(Category category, List<short> categoryIds)
        {
            var subcategories = await _appDbContext.Categories
                .Where(c => c.ParentCategoryId == category.Id && c.IsActive)
                .ToListAsync();

            foreach (var subcategory in subcategories)
            {
                categoryIds.Add(subcategory.Id);
                await CollectSubcategoryIdsAsync(subcategory, categoryIds);
            }
        }

        //public async Task<Result<CategoryHierarchyDto>> UpdateCategoryHierarchyAsync(
        //    CategoryHierarchyDto? categoryHierarchy,
        //    string? name,
        //    bool? isActive)
        //{
        //    using var transaction = await _appDbContext.Database.BeginTransactionAsync();

        //    try
        //    {
        //        // Валидация входных параметров
        //        if (categoryHierarchy == null)
        //        {
        //            return Result<CategoryHierarchyDto>.Failure("Иерархия категории не может быть null");
        //        }

        //        if (string.IsNullOrWhiteSpace(categoryHierarchy.Name) && string.IsNullOrWhiteSpace(name))
        //        {
        //            return Result<CategoryHierarchyDto>.Failure("Название категории не может быть пустым");
        //        }

        //        // Находим категорию в базе
        //        var category = await _appDbContext.Categories
        //            .FirstOrDefaultAsync(c => c.Name == categoryHierarchy.Name && c.IsActive);

        //        if (category == null)
        //        {
        //            return Result<CategoryHierarchyDto>.Failure("Категория не найдена");
        //        }

        //        // Обновляем поля, если они указаны
        //        if (!string.IsNullOrWhiteSpace(name))
        //        {
        //            // Проверяем уникальность нового имени на том же уровне
        //            var nameExists = await _appDbContext.Categories
        //                .AnyAsync(c => c.Name == name &&
        //                              c.ParentCategoryId == category.ParentCategoryId &&
        //                              c.Id != category.Id);

        //            if (nameExists)
        //            {
        //                return Result<CategoryHierarchyDto>.Failure($"Категория с именем '{name}' уже существует на этом уровне");
        //            }

        //            category.Name = name.Trim();
        //        }

        //        if (isActive.HasValue)
        //        {
        //            category.IsActive = isActive.Value;

        //            if (!isActive.Value)
        //            {
        //                await DeactivateSubcategoriesAsync(category.Id);
        //            }
        //        }

        //        category.UpdatedAt = DateTime.UtcNow;

        //        _appDbContext.Categories.Update(category);
        //        await _appDbContext.SaveChangesAsync();
        //        await transaction.CommitAsync();

        //        // Возвращаем обновленную иерархию
        //        var updatedHierarchy = await BuildCategoryHierarchyFromDbAsync(category.Id);

        //        return updatedHierarchy != null
        //            ? Result<CategoryHierarchyDto>.Success(updatedHierarchy)
        //            : Result<CategoryHierarchyDto>.Failure("Категория не найдена");
        //    }
        //    catch (Exception ex)
        //    {
        //        await transaction.RollbackAsync();
        //        return Result<CategoryHierarchyDto>.Failure($"Ошибка при обновлении категории: {ex.Message}");
        //    }
        //}

        //// Вспомогательный метод для деактивации подкатегорий
        //private async Task DeactivateSubcategoriesAsync(short categoryId)
        //{
        //    var subcategories = await _appDbContext.Categories
        //        .Where(c => c.ParentCategoryId == categoryId)
        //        .ToListAsync();

        //    foreach (var subcategory in subcategories)
        //    {
        //        subcategory.IsActive = false;
        //        subcategory.UpdatedAt = DateTime.UtcNow;

        //        // Рекурсивно деактивируем вложенные категории
        //        await DeactivateSubcategoriesAsync(subcategory.Id);
        //    }

        //    _appDbContext.Categories.UpdateRange(subcategories);
        //}
    }
}
