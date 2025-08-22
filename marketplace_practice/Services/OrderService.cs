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
            Dictionary<long, int> cartItemQuantities,
            string fullName,
            string phoneNumber,
            string country,
            string postalCode,
            Currency currency = Currency.RUB)
        {
            var userId = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out var buyerId))
            {
                return Result<OrderDto>.Failure("Не удалось идентифицировать пользователя");
            }

            if (cartItemQuantities.Any(pq => pq.Value <= 0))
            {
                return Result<OrderDto>.Failure("Количество товаров должно быть больше 0");
            }

            using var transaction = await _appDbContext.Database.BeginTransactionAsync();

            try
            {
                // Получение элементов корзины с проекцией в DTO
                var cartItemIds = cartItemQuantities.Keys.ToList();
                var cartItemsInfo = await _appDbContext.CartItems
                    .Where(ci => cartItemIds.Contains(ci.Id) &&
                                ci.Cart.UserId == buyerId &&
                                ci.Product.IsActive &&
                                ci.OrderItem == null)
                    .Select(ci => new
                    {
                        CartItemId = ci.Id,
                        ProductId = ci.Product.Id,
                        ProductUserId = ci.Product.UserId,
                        ProductName = ci.Product.Name,
                        ProductPrice = ci.Product.Price,
                        ProductCurrency = ci.Product.Currency
                    })
                    .ToListAsync();

                if (cartItemsInfo.Count != cartItemIds.Count)
                {
                    var missingIds = cartItemIds.Except(cartItemsInfo.Select(x => x.CartItemId));
                    return Result<OrderDto>.Failure($"Следующие элементы корзины не найдены: {string.Join(", ", missingIds)}");
                }

                // Получение данных покупателя
                var buyer = await _appDbContext.Users
                    .Where(u => u.Id == buyerId)
                    .Select(u => new UserBriefInfoDto
                    {
                        Id = u.Id,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Email = u.Email!,
                        PhoneNumber = u.PhoneNumber
                    })
                    .FirstOrDefaultAsync();

                if (buyer == null)
                {
                    return Result<OrderDto>.Failure("Покупатель не найден");
                }

                // Создание заказа
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

                // Добавление OrderItems
                foreach (var cartItemInfo in cartItemsInfo)
                {
                    order.OrderItems.Add(new OrderItem
                    {
                        CartItemId = cartItemInfo.CartItemId,
                        Quantity = cartItemQuantities[cartItemInfo.CartItemId],
                        Currency = currency,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                await _appDbContext.Orders.AddAsync(order);
                await _appDbContext.SaveChangesAsync();

                // Создание DTO
                var orderDto = await _appDbContext.Orders
                    .AsNoTracking()
                    .Where(o => o.Id == order.Id)
                    .Select(o => new OrderDto
                    {
                        Id = o.Id,
                        User = buyer,
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
                                Id = oi.CartItem.Product.Id,
                                UserId = oi.CartItem.Product.UserId,
                                Name = oi.CartItem.Product.Name,
                                Price = oi.CartItem.Product.Price,
                                Currency = oi.CartItem.Product.Currency,
                                ProductImages = oi.CartItem.Product.ProductImages
                                .Select(pi => new ProductImageDto
                                {
                                    Url = pi.Url,
                                    IsMain = pi.IsMain
                                }).ToList()
                            }
                        }).ToList(),
                        OrderDetail = new OrderDetailDto
                        {
                            FullName = o.OrderDetail.FullName,
                            PhoneNumber = o.OrderDetail.PhoneNumber,
                            Country = o.OrderDetail.Country,
                            PostalCode = o.OrderDetail.PostalCode
                        }
                    })
                    .FirstOrDefaultAsync();

                await transaction.CommitAsync();

                if (orderDto == null)
                {
                    return Result<OrderDto>.Failure("Ошибка при создании DTO заказа");
                }

                // Вычисление общей суммы заказа
                orderDto.TotalAmount = orderDto.OrderItems
                    .Sum(oi => oi.Product.Price * oi.Quantity);

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
                                Id = oi.CartItem.Product.Id,
                                UserId = oi.CartItem.Product.UserId,
                                Name = oi.CartItem.Product.Name,
                                Price = oi.CartItem.Product.Price,
                                Currency = oi.CartItem.Product.Currency,
                                ProductImages = oi.CartItem.Product.ProductImages
                                .Select(pi => new ProductImageDto
                                {
                                    Url = pi.Url,
                                    IsMain = pi.IsMain
                                }).ToList()
                            }
                        }).ToList(),
                        OrderDetail = new OrderDetailDto
                        {
                            FullName = o.OrderDetail.FullName,
                            PhoneNumber = o.OrderDetail.PhoneNumber,
                            Country = o.OrderDetail.Country,
                            PostalCode = o.OrderDetail.PostalCode
                        },
                        LoyaltyTransaction = o.LoyaltyTransaction != null
                            ? new LoyaltyTransactionDto
                            {
                                UserId = o.LoyaltyTransaction.UserId,
                                Type = o.LoyaltyTransaction.Type.ToString(),
                                Points = o.LoyaltyTransaction.Points,
                                Description = o.LoyaltyTransaction.Description,
                                CreatedAt = o.LoyaltyTransaction.CreatedAt
                            }
                            : null,
                        Payment = o.Payment != null
                            ? new PaymentDto
                            {
                                Id = o.Payment.Id,
                                ProviderName = o.Payment.ProviderName,
                                ProviderPaymentId = o.Payment.ProviderPaymentId,
                                Amount = o.Payment.Amount,
                                Currency = o.Payment.Currency,
                                CreatedAt = o.Payment.CreatedAt
                            }
                            : null
                    })
                    .FirstOrDefaultAsync();

                if (orderDto == null)
                {
                    return Result<OrderDto>.Failure("Заказ не найден");
                }

                // Проверка прав доступа
                if (orderDto.User.Id != currentUserId)
                {
                    return Result<OrderDto>.Failure("Отказано в доступе");
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

        public async Task<Result<ICollection<OrderDto>>> GetOrderListAsync(
            ClaimsPrincipal userPrincipal,
            string? targetUserId = null)
        {
            // Получение текущего пользователя
            var userId = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out var currentUserId))
            {
                return Result<ICollection<OrderDto>>.Failure("Не удалось идентифицировать пользователя");
            }

            // Определение ID целевого пользователя
            long? resolvedUserId = null;

            if (!string.IsNullOrEmpty(targetUserId))
            {
                // Проверка прав администратора
                if (userId != targetUserId && !userPrincipal.IsInRole("MainAdmin"))
                {
                    return Result<ICollection<OrderDto>>.Failure("Отказано в доступе");
                }

                if (!long.TryParse(targetUserId, out var parsedUserId))
                {
                    return Result<ICollection<OrderDto>>.Failure("Некорректный формат ID пользователя");
                }
                resolvedUserId = parsedUserId;
            }
            else
            {
                resolvedUserId = currentUserId;
            }

            try
            {
                // Оптимизированный запрос с проекцией в DTO через CartItem
                var orderDtos = await _appDbContext.Orders
                    .AsNoTracking()
                    .Where(o => o.UserId == resolvedUserId)
                    .OrderByDescending(o => o.CreatedAt)
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
                                Id = oi.CartItem.Product.Id,
                                UserId = oi.CartItem.Product.UserId,
                                Name = oi.CartItem.Product.Name,
                                Price = oi.CartItem.Product.Price,
                                Currency = oi.CartItem.Product.Currency,
                                ProductImages = oi.CartItem.Product.ProductImages
                                    .Select(pi => new ProductImageDto
                                    {
                                        Url = pi.Url,
                                        IsMain = pi.IsMain
                                    }).ToList()
                            }
                        }).ToList(),
                        OrderDetail = new OrderDetailDto
                        {
                            FullName = o.OrderDetail.FullName,
                            PhoneNumber = o.OrderDetail.PhoneNumber,
                            Country = o.OrderDetail.Country,
                            PostalCode = o.OrderDetail.PostalCode
                        },
                        LoyaltyTransaction = o.LoyaltyTransaction != null
                            ? new LoyaltyTransactionDto
                            {
                                UserId = o.LoyaltyTransaction.UserId,
                                Type = o.LoyaltyTransaction.Type.ToString(),
                                Points = o.LoyaltyTransaction.Points,
                                Description = o.LoyaltyTransaction.Description,
                                CreatedAt = o.LoyaltyTransaction.CreatedAt
                            }
                            : null,
                        Payment = o.Payment != null
                            ? new PaymentDto
                            {
                                Id = o.Payment.Id,
                                ProviderName = o.Payment.ProviderName,
                                ProviderPaymentId = o.Payment.ProviderPaymentId,
                                Amount = o.Payment.Amount,
                                Currency = o.Payment.Currency,
                                CreatedAt = o.Payment.CreatedAt
                            }
                            : null
                    })
                    .ToListAsync();

                // Вычисление общей суммы заказов
                foreach (var orderDto in orderDtos)
                {
                    orderDto.TotalAmount = orderDto.OrderItems
                        .Sum(oi => oi.Product.Price * oi.Quantity);
                }

                return Result<ICollection<OrderDto>>.Success(orderDtos);
            }
            catch
            {
                throw;
            }
        }

        public async Task<Result<OrderDto>> UpdateOrderAsync(
            ClaimsPrincipal userPrincipal,
            string orderId,
            Dictionary<long, int>? updatedCartItems,
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
                // Получение данных заказа с включением необходимых связей
                var order = await _appDbContext.Orders
                    .Include(o => o.OrderDetail)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.CartItem)
                    .Include(o => o.Payment)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                    return Result<OrderDto>.Failure("Заказ не найден");

                if (order.UserId != currentUserId)
                    return Result<OrderDto>.Failure("Отказано в доступе");

                // Проверка возможности изменения заказа в зависимости от статуса
                if (!CanOrderBeModified(order.Status))
                {
                    return Result<OrderDto>
                        .Failure($"Невозможно изменить заказ со статусом '{order.Status.GetDisplayName()}'. " +
                            "Разрешено изменять только заказы со статусами: Новый, В обработке.");
                }

                // Обновление деталей доставки
                if (fullName != null) order.OrderDetail.FullName = fullName;
                if (phoneNumber != null) order.OrderDetail.PhoneNumber = phoneNumber;
                if (country != null) order.OrderDetail.Country = country;
                if (postalCode != null) order.OrderDetail.PostalCode = postalCode;

                // Обновление состава заказа (если указано)
                if (updatedCartItems != null)
                {
                    await UpdateOrderItemsAsync(order, updatedCartItems);
                }

                await _appDbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                // Получение обновленного заказа через GetOrderByIdAsync
                return await GetOrderByIdAsync(userPrincipal, orderId);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private bool CanOrderBeModified(OrderStatus status)
        {
            return status == OrderStatus.New || status == OrderStatus.Processing;
        }

        private async Task UpdateOrderItemsAsync(Order order, Dictionary<long, int> updatedCartItems)
        {
            // Удаление отсутствующих позиций
            var cartItemIdsToKeep = updatedCartItems.Keys.ToList();
            var itemsToRemove = order.OrderItems
                .Where(oi => !cartItemIdsToKeep.Contains(oi.CartItemId))
                .ToList();

            foreach (var item in itemsToRemove)
            {
                order.OrderItems.Remove(item);
            }

            // Обновление существующих позиций
            foreach (var orderItem in order.OrderItems.Where(oi => cartItemIdsToKeep.Contains(oi.CartItemId)))
            {
                if (updatedCartItems.TryGetValue(orderItem.CartItemId, out var newQuantity))
                {
                    orderItem.Quantity = newQuantity;
                    orderItem.UpdatedAt = DateTime.UtcNow;
                }
            }

            // Добавление новых позиций (если такие есть)
            var existingCartItemIds = order.OrderItems.Select(oi => oi.CartItemId).ToList();
            var newCartItemIds = cartItemIdsToKeep.Except(existingCartItemIds).ToList();

            if (newCartItemIds.Any())
            {
                // Получение информации о CartItems
                var cartItemsInfo = await _appDbContext.CartItems
                    .Include(ci => ci.Product)
                    .Where(ci => newCartItemIds.Contains(ci.Id))
                    .ToListAsync();

                foreach (var cartItemInfo in cartItemsInfo)
                {
                    if (updatedCartItems.TryGetValue(cartItemInfo.Id, out var quantity))
                    {
                        order.OrderItems.Add(new OrderItem
                        {
                            CartItemId = cartItemInfo.Id,
                            Quantity = quantity,
                            Currency = cartItemInfo.Product.Currency,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                }
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
                if (order.UserId != currentUserId)
                {
                    return Result<string>.Failure("Отказано в доступе");
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
