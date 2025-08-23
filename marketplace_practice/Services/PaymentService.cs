using marketplace_practice.Models;
using marketplace_practice.Models.Enums;
using marketplace_practice.Services.dto.Orders;
using marketplace_practice.Services.interfaces;
using marketplace_practice.Services.service_models;
using Microsoft.EntityFrameworkCore;

namespace marketplace_practice.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly AppDbContext _appDbContext;
        private readonly IPDFService _pdfService;
        private readonly IEmailService _emailService;

        public PaymentService(AppDbContext appDbContext, IPDFService pdfService, IEmailService emailService)
        {
            _appDbContext = appDbContext;
            _pdfService = pdfService;
            _emailService = emailService;
        }

        public async Task<Result<PaymentDto>> ProcessPaymentAsync(
            string orderId,
            string providerName,
            decimal amount,
            string currency)
        {
            // Валидация ID заказа
            if (!long.TryParse(orderId, out var id))
            {
                return Result<PaymentDto>.Failure("Неверный формат ID заказа");
            }

            using var transaction = await _appDbContext.Database.BeginTransactionAsync();

            try
            {
                // Получение необходиой информации о товарах
                var orderProducts = await _appDbContext.OrderItems
                    .Where(oi => oi.OrderId == id)
                    .Select(oi => new
                    {
                        oi.Quantity,
                        Product = oi.CartItem.Product
                    })
                    .ToListAsync();

                // Проверка статуса заказа
                var orderStatus = await _appDbContext.Orders
                    .AsNoTracking()
                    .Where(o => o.Id == id)
                    .Select(o => o.Status)
                    .FirstOrDefaultAsync();

                if (orderStatus == OrderStatus.Paid)
                {
                    return Result<PaymentDto>.Failure("Заказ уже оплачен");
                }

                // Проверка доступности товаров
                foreach (var item in orderProducts)
                {
                    if (item.Product.StockQuantity < item.Quantity)
                    {
                        await transaction.RollbackAsync();
                        return Result<PaymentDto>.Failure(new List<string>
                        {
                            new string("Недостаточно товара на складе"),
                            new string($"Заказано: {item.Quantity}, доступно: {item.Product.StockQuantity}")
                        });
                    }
                }

                // Уменьшение количества товара на складе
                foreach (var item in orderProducts)
                {
                    item.Product.StockQuantity -= item.Quantity;
                    item.Product.UpdatedAt = DateTime.UtcNow;
                }

                // Обновление статуса заказа
                var order = await _appDbContext.Orders
                    .Where(o => o.Id == id)
                    .Include(o => o.User)
                    .FirstOrDefaultAsync();

                if (order == null)
                {
                    await transaction.RollbackAsync();
                    return Result<PaymentDto>.Failure("Заказ не найден");
                }

                order!.Status = OrderStatus.Paid;
                order.UpdatedAt = DateTime.UtcNow;

                // Создание записи о платеже
                var payment = new Payment
                {
                    OrderId = id,
                    ProviderName = providerName,
                    ProviderPaymentId = Guid.NewGuid().ToString(),
                    Amount = amount,
                    Currency = currency.ParseFromDisplayName<Currency>(),
                    CreatedAt = DateTime.UtcNow,
                };

                // Обновление статуса заказа
                order.Status = OrderStatus.Paid;
                order.UpdatedAt = DateTime.UtcNow;

                await _appDbContext.Payments.AddAsync(payment);
                await _appDbContext.SaveChangesAsync();

                // Сбор данных для чека
                var receipt = new ReceiptModel
                {
                    ReceiptNumber = $"INV-{DateTime.UtcNow:yyyy-MM-dd}-{payment.Id}",
                    StoreName = "Marketplace Practice",
                    CustomerName = providerName,
                    IssueDate = payment.CreatedAt,
                    Items = new List<ReceiptItem>()
                };

                foreach(var orderProduct in orderProducts)
                {
                    receipt.Items.Add(new ReceiptItem
                    {
                        ProductName = orderProduct.Product.Name,
                        Quantity = orderProduct.Quantity,
                        UnitPrice = orderProduct.Product.Price,
                    });
                }

                // Генерирация и сохранение PDF
                var result = await _pdfService.SaveReceiptAsPdfAsync(receipt, "receipts");

                if (string.IsNullOrEmpty(result.Item1))
                {
                    await transaction.RollbackAsync();
                    return Result<PaymentDto>.Failure("Ошибка при создании чека");
                }

                // Отправка чека на электронную почту
                var customerEmail = order.User.Email;
                var customerName = order.User.FirstName;

                //await _emailService.SendPdfReceiptAsync(
                //    email: customerEmail!,
                //    firstName: customerName,
                //    pdfBytes: result.Item2,
                //    fileName: $"receipt_{payment.Id}.pdf",
                //    subject: $"Чек по заказу №{order.Id}"
                //);

                await transaction.CommitAsync();

                var paymentDto = new PaymentDto
                {
                    Id = payment.Id,
                    OrderId = payment.OrderId,
                    ProviderName = payment.ProviderName,
                    ProviderPaymentId = payment.ProviderPaymentId,
                    Amount = payment.Amount,
                    Currency = payment.Currency.GetDisplayName(),
                    CreatedAt = payment.CreatedAt,
                    ReceiptUrl = result.Item1 // только для тестов
                };

                return Result<PaymentDto>.Success(paymentDto);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
