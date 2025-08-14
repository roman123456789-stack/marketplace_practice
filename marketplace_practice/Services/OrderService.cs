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
    public class OrderService : IOrderService
    {
        private readonly AppDbContext _appDbContext;

        public OrderService(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task<Result<OrderDto>> CreateOrderAsync(
            ClaimsPrincipal userPrincipal,
            Dictionary<long, int> productQuantities,
            Currency currency,
            string fullName,
            string phoneNumber,
            string country,
            string postalCode)
        {
            // Получение пользователя из ClaimsPrincipal
            var userId = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out var buyerId))
            {
                return Result<OrderDto>.Failure("Не удалось идентифицировать пользователя");
            }

            if (productQuantities.Any(pq => pq.Value <= 0))
            {
                return Result<OrderDto>.Failure("Количество товаров должно быть больше 0");
            }

            // Начало транзакции
            using var transaction = await _appDbContext.Database.BeginTransactionAsync();

            try
            {
                // Получение всех продуктов
                var productIds = productQuantities.Keys.ToList();
                var products = await _appDbContext.Products
                    .Where(p => productIds.Contains(p.Id) && p.IsActive)
                    .Select(p => new { p.Id, p.UserId, p.Name, p.Price, p.Currency })
                    .ToListAsync();

                // Проверка наличия всех продуктов
                if (products.Count != productIds.Count)
                {
                    var missingIds = productIds.Except(products.Select(p => p.Id));
                    return Result<OrderDto>.Failure($"Следующие товары не найдены или недоступны: {string.Join(", ", missingIds)}");
                }

                // Проверка валюты продуктов <--- нужно добавить позже
                // какая-то логика...

                // Получаем данные покупателя
                var buyer = await _appDbContext.Users
                    .Where(u => u.Id == buyerId)
                    .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email, u.PhoneNumber })
                    .FirstOrDefaultAsync();

                if (buyer == null)
                {
                    return Result<OrderDto>.Failure("Покупатель не найден");
                }

                // Создаем заказ
                var order = new Order
                {
                    UserId = buyerId,
                    Status = OrderStatus.New,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    OrderDetail = new OrderDetail
                    {
                        FullName = fullName,
                        PhoneNumber = phoneNumber,
                        Country = country,
                        PostalCode = postalCode
                    }
                };

                // Добавляем все товары в заказ
                foreach (var product in products)
                {
                    order.OrderItems.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        Quantity = productQuantities[product.Id],
                        Currency = currency,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                // Сохраняем заказ
                await _appDbContext.Orders.AddAsync(order);
                await _appDbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                // Формируем DTO
                var orderDto = new OrderDto
                {
                    Id = order.Id,
                    User = new UserBriefInfoDto
                    {
                        Id = buyer.Id,
                        FirstName = buyer.FirstName,
                        LastName = buyer.LastName,
                        Email = buyer.Email!,
                        PhoneNumber = buyer.PhoneNumber
                    },
                    Status = order.Status.GetDisplayName(),
                    CreatedAt = order.CreatedAt,
                    UpdatedAt = order.UpdatedAt,
                    OrderItems = order.OrderItems.Select(oi => new OrderItemDto
                    {
                        Quantity = oi.Quantity,
                        Currency = oi.Currency.GetDisplayName(),
                        CreatedAt = oi.CreatedAt,
                        UpdatedAt = oi.UpdatedAt,
                        Product = new ProductBriefInfoDto
                        {
                            Id = oi.ProductId,
                            UserId = products.First(p => p.Id == oi.ProductId).UserId,
                            Name = products.First(p => p.Id == oi.ProductId).Name,
                            Price = products.First(p => p.Id == oi.ProductId).Price,
                            Currency = currency
                        }
                    }).ToList(),
                    OrderDetail = new OrderDetailDto
                    {
                        FullName = fullName,
                        PhoneNumber = phoneNumber,
                        Country = country,
                        PostalCode = postalCode
                    },
                    TotalAmount = order.OrderItems.Sum(oi =>
                        products.First(p => p.Id == oi.ProductId).Price * oi.Quantity)
                };

                return Result<OrderDto>.Success(orderDto);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Result<OrderDto>> GetOrderByIdAsync(ClaimsPrincipal userPrincipal, string orderId)
        {
            // Валидация входных параметров
            if (string.IsNullOrWhiteSpace(orderId))
            {
                return Result<OrderDto>.Failure("ID заказа не может быть пустым");
            }

            if (!long.TryParse(orderId, out var id))
            {
                return Result<OrderDto>.Failure("Неверный формат ID заказа");
            }

            // Получение текущего пользователя
            var userId = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out var currentUserId))
            {
                return Result<OrderDto>.Failure("Не удалось идентифицировать пользователя");
            }

            try
            {
                // Оптимизированный запрос с проекцией в DTO
                var orderDto = await _appDbContext.Orders
                    .AsNoTracking()
                    .Where(o => o.Id == id)
                    .Select(o => new OrderDto
                    {
                        Id = o.Id,
                        User = new UserBriefInfoDto
                        {
                            Id = o.User.Id,
                            FirstName = o.User.FirstName,
                            LastName = o.User.LastName,
                            Email = o.User.Email!,
                            PhoneNumber = o.User.PhoneNumber
                        },
                        Status = o.Status.GetDisplayName(),
                        CreatedAt = o.CreatedAt,
                        UpdatedAt = o.UpdatedAt,
                        OrderItems = o.OrderItems.Select(oi => new OrderItemDto
                        {
                            Quantity = oi.Quantity,
                            Currency = oi.Currency.GetDisplayName(),
                            CreatedAt = oi.CreatedAt,
                            UpdatedAt = oi.UpdatedAt,
                            Product = new ProductBriefInfoDto
                            {
                                Id = oi.Product.Id,
                                UserId = oi.Product.UserId,
                                Name = oi.Product.Name,
                                Price = oi.Product.Price,
                                Currency = oi.Product.Currency
                            }
                        }).ToList(),
                        OrderDetail = new OrderDetailDto
                        {
                            FullName = o.OrderDetail.FullName,
                            PhoneNumber = o.OrderDetail.PhoneNumber,
                            Country = o.OrderDetail.Country,
                            PostalCode = o.OrderDetail.PostalCode
                        },
                        LoyaltyTransaction = o.LoyaltyTransaction != null ? new LoyaltyTransactionDto
                        {
                            UserId = o.LoyaltyTransaction.UserId,
                            Type = o.LoyaltyTransaction.Type.ToString(),
                            Points = o.LoyaltyTransaction.Points,
                            Description = o.LoyaltyTransaction.Description,
                            CreatedAt = o.LoyaltyTransaction.CreatedAt
                        } : null,
                        Payment = o.Payment != null ? new PaymentDto
                        {
                            Id = o.Payment.Id,
                            ProviderName = o.Payment.ProviderName,
                            ProviderPaymentId = o.Payment.ProviderPaymentId,
                            Amount = o.Payment.Amount,
                            Currency = o.Payment.Currency,
                            CreatedAt = o.Payment.CreatedAt
                        } : null
                    })
                    .FirstOrDefaultAsync();

                if (orderDto == null)
                {
                    return Result<OrderDto>.Failure("Заказ не найден");
                }

                // Проверка прав доступа
                if (orderDto.User.Id != currentUserId && !userPrincipal.IsInRole("Admin"))
                {
                    return Result<OrderDto>.Failure("Нет доступа к данному заказу");
                }

                // Вычисление общей суммы заказа
                orderDto.TotalAmount = orderDto.OrderItems
                    .Sum(oi => oi.Product.Price * oi.Quantity);

                return Result<OrderDto>.Success(orderDto);
            }
            catch
            {
                throw;
            }
        }

        public async Task<Result<OrderDto>> UpdateOrderAsync(
            ClaimsPrincipal userPrincipal,
            string orderId,
            OrderStatus? status,
            Dictionary<long, int>? updatedItems,
            string? fullName,
            string? phoneNumber,
            string? country,
            string? postalCode)
        {
            // Валидация параметров
            if (!long.TryParse(orderId, out var id))
                return Result<OrderDto>.Failure("Неверный формат ID заказа");

            var userId = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userId, out var currentUserId))
                return Result<OrderDto>.Failure("Не удалось идентифицировать пользователя");

            // Начало транзакции
            using var transaction = await _appDbContext.Database.BeginTransactionAsync();

            try
            {
                // Получение данных заказа
                var order = await _appDbContext.Orders
                    .Select(o => new
                    {
                        o.Id,
                        o.UserId,
                        o.Status,
                        PaymentExists = o.Payment != null
                    })
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                    return Result<OrderDto>.Failure("Заказ не найден");

                if (order.UserId != currentUserId && !userPrincipal.IsInRole("Admin"))
                    return Result<OrderDto>.Failure("Нет доступа к данному заказу");

                // Обновление только необходимые полей
                var orderToUpdate = new Order { Id = id };
                _appDbContext.Orders.Attach(orderToUpdate);

                if (status.HasValue)
                {
                    if (status == OrderStatus.Completed && !order.PaymentExists)
                        return Result<OrderDto>.Failure("Нельзя завершить неоплаченный заказ");

                    orderToUpdate.Status = status.Value;
                    orderToUpdate.UpdatedAt = DateTime.UtcNow;
                }

                // Обновление деталей доставки
                if (fullName != null || phoneNumber != null || country != null || postalCode != null)
                {
                    var detailToUpdate = await _appDbContext.OrderDetails
                        .FirstOrDefaultAsync(od => od.OrderId == id);

                    _appDbContext.OrderDetails.Attach(detailToUpdate!);

                    if (fullName != null) detailToUpdate!.FullName = fullName;
                    if (phoneNumber != null) detailToUpdate!.PhoneNumber = phoneNumber;
                    if (country != null) detailToUpdate!.Country = country;
                    if (postalCode != null) detailToUpdate!.PostalCode = postalCode;
                }

                // Обновление состава заказа
                if (updatedItems != null)
                {
                    await UpdateOrderItemsAsync(id, updatedItems);
                }

                await _appDbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                // Получение обновленного заказа
                return await GetOrderByIdAsync(userPrincipal, orderId);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task UpdateOrderItemsAsync(long orderId, Dictionary<long, int> updatedItems)
        {
            // Удаление отсутствующих позиций
            var productIdsToKeep = updatedItems.Keys.ToList();
            await _appDbContext.OrderItems
                .Where(oi => oi.OrderId == orderId && !productIdsToKeep.Contains(oi.ProductId))
                .ExecuteDeleteAsync();

            // Обновление существующих позиций
            var existingItems = await _appDbContext.OrderItems
                .Where(oi => oi.OrderId == orderId && productIdsToKeep.Contains(oi.ProductId))
                .ToListAsync();

            foreach (var item in existingItems)
            {
                item.Quantity = updatedItems[item.ProductId];
                item.UpdatedAt = DateTime.UtcNow;
            }

            // Добавление новых позиций
            var existingProductIds = existingItems.Select(oi => oi.ProductId).ToList();
            var newProductIds = productIdsToKeep.Except(existingProductIds).ToList();

            if (newProductIds.Any())
            {
                var newItems = await _appDbContext.Products
                    .Where(p => newProductIds.Contains(p.Id))
                    .Select(p => new OrderItem
                    {
                        OrderId = orderId,
                        ProductId = p.Id,
                        Quantity = updatedItems[p.Id],
                        Currency = p.Currency,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    })
                    .ToListAsync();

                await _appDbContext.OrderItems.AddRangeAsync(newItems);
            }
        }

        public async Task<Result<string>> DeleteOrderAsync(ClaimsPrincipal userPrincipal, string orderId)
        {
            // Валидация входных параметров
            if (string.IsNullOrWhiteSpace(orderId))
            {
                return Result<string>.Failure("ID заказа не может быть пустым");
            }

            if (!long.TryParse(orderId, out var id))
            {
                return Result<string>.Failure("Неверный формат ID заказа");
            }

            // Получение текущего пользователя
            var userId = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out var currentUserId))
            {
                return Result<string>.Failure("Не удалось идентифицировать пользователя");
            }

            // Начало транзакции
            using var transaction = await _appDbContext.Database.BeginTransactionAsync();

            try
            {
                // Получаем заказ только с необходимыми полями для проверки
                var order = await _appDbContext.Orders
                    .Select(o => new
                    {
                        o.Id,
                        o.UserId,
                        o.Status,
                        CanDelete = o.Status == OrderStatus.New
                                || o.Status == OrderStatus.Cancelled
                                || o.Status == OrderStatus.Refunded
                    })
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    return Result<string>.Failure("Заказ не найден");
                }

                // Проверка прав доступа
                if (order.UserId != currentUserId && !userPrincipal.IsInRole("Admin"))
                {
                    return Result<string>.Failure("Нет доступа к данному заказу");
                }

                // Проверка статуса заказа
                if (!order.CanDelete)
                {
                    return Result<string>.Failure(
                        $"Заказы со статусом {order.Status.GetDisplayName()} нельзя удалять. " +
                        "Доступно удаление только для статусов: Новый, Отменен, Возврат");
                }

                // Удаление заказа
                var orderToDelete = new Order { Id = id };
                _appDbContext.Orders.Attach(orderToDelete);
                _appDbContext.Orders.Remove(orderToDelete);

                await _appDbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return Result<string>.Success($"Заказ с ID {orderId} успешно удален");
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23503")
            {
                await transaction.RollbackAsync();
                return Result<string>.Failure("Нельзя удалить заказ, так как он используется в других записях");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
