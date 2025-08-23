using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace marketplace_practice.Controllers.dto.Products
{
    public class CreateProductDto
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public string Price { get; set; }
        public string? PromotionalPrice { get; set; }
        public string? Size { get; set; }
        public string Currency { get; set; }
        public string CategoryHierarchy { get; set; }
        public List<IFormFile>? Images { get; set; }
        public string StockQuantity { get; set; }

        public ValidationResult ValidateForm()
        {
            var errors = new Dictionary<string, List<string>>();

            // Валидация Name
            if (string.IsNullOrEmpty(Name))
            {
                AddError(errors, "Name", "Поле 'Name' не может быть пустым");
            }
            else if (Name.Length > 50)
            {
                AddError(errors, "Name", "Поле не может быть длиннее 50 символов");
            }

            // Валидация Price
            if (string.IsNullOrEmpty(Price))
            {
                AddError(errors, "Price", "Поле 'Price' не может быть пустым");
            }
            else if (!decimal.TryParse(Price, out decimal priceValue) || priceValue <= 0)
            {
                AddError(errors, "Price", "Цена должна быть больше 0");
            }

            // Валидация PromotionalPrice
            if (!string.IsNullOrEmpty(PromotionalPrice))
            {
                if (!decimal.TryParse(PromotionalPrice, out decimal promoPriceValue) || promoPriceValue <= 0)
                {
                    AddError(errors, "PromotionalPrice", "Цена должна быть больше 0");
                }
            }

            // Валидация Size
            if (!string.IsNullOrEmpty(Size))
            {
                if (!short.TryParse(Size, out short sizeValue) || sizeValue <= 0)
                {
                    AddError(errors, "Size", "Размер должен быть больше 0");
                }
            }

            // Валидация Currency
            if (string.IsNullOrEmpty(Currency))
            {
                AddError(errors, "Currency", "Поле 'Currency' не может быть пустым");
            }
            else if (!new[] { "RUB", "USD", "EUR", "CNY" }.Contains(Currency.ToUpper()))
            {
                AddError(errors, "Currency", "Неверный формат валюты (допустимо: RUB, USD, EUR, CNY)");
            }

            // Валидация CategoryHierarchy
            if (string.IsNullOrEmpty(CategoryHierarchy))
            {
                AddError(errors, "CategoryHierarchy", "Поле 'CategoryHierarchy' не может быть пустым");
            }
            else
            {
                try
                {
                    var categories = GetCategoryHierarchy();
                    if (categories == null || !categories.Any())
                    {
                        AddError(errors, "CategoryHierarchy", "Не удалось десериализовать иерархию категорий");
                    }
                    else
                    {
                        foreach (var category in categories)
                        {
                            var categoryErrors = category.Validate();
                            foreach (var error in categoryErrors)
                            {
                                AddError(errors, "CategoryHierarchy", error);
                            }
                        }
                    }
                }
                catch (ArgumentException ex)
                {
                    AddError(errors, "CategoryHierarchy", ex.Message);
                }
                catch (JsonException)
                {
                    AddError(errors, "CategoryHierarchy", "Неверный формат JSON для CategoryHierarchy");
                }
            }

            // Валидация StockQuantity
            if (string.IsNullOrEmpty(StockQuantity))
            {
                AddError(errors, "StockQuantity", "Поле 'StockQuantity' не может быть пустым");
            }
            else if (!int.TryParse(StockQuantity, out int stockValue) || stockValue < 0)
            {
                AddError(errors, "StockQuantity", "Количество не может быть отрицательным");
            }

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        private void AddError(Dictionary<string, List<string>> errors, string field, string message)
        {
            if (!errors.ContainsKey(field))
            {
                errors[field] = new List<string>();
            }
            errors[field].Add(message);
        }

        // Метод для десериализации строки в ICollection<CategoryHierarchyDto>
        public ICollection<CategoryHierarchyDto>? GetCategoryHierarchy()
        {
            if (string.IsNullOrEmpty(CategoryHierarchy))
                return null;

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                // Пытаемся десериализовать как массив
                try
                {
                    return JsonSerializer.Deserialize<ICollection<CategoryHierarchyDto>>(CategoryHierarchy, options);
                }
                catch (JsonException)
                {
                    // Если не массив, пытаемся десериализовать как одиночный объект и обернуть в коллекцию
                    var singleItem = JsonSerializer.Deserialize<CategoryHierarchyDto>(CategoryHierarchy, options);
                    return singleItem != null ? new List<CategoryHierarchyDto> { singleItem } : null;
                }
            }
            catch (JsonException ex)
            {
                throw new ArgumentException("Неверный формат JSON для CategoryHierarchy", ex);
            }
        }

        // Методы для преобразования строковых значений в числовые
        public decimal GetPrice() => decimal.Parse(Price);
        public decimal GetPromotionalPrice() => string.IsNullOrEmpty(PromotionalPrice) ? 0 : decimal.Parse(PromotionalPrice);
        public short GetSize() => string.IsNullOrEmpty(Size) ? (short)0 : short.Parse(Size);
        public int GetStockQuantity() => int.Parse(StockQuantity);
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public Dictionary<string, List<string>> Errors { get; set; } = new Dictionary<string, List<string>>();
    }
}
