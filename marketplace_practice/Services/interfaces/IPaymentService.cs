using marketplace_practice.Services.dto.Orders;
using marketplace_practice.Services.service_models;

namespace marketplace_practice.Services.interfaces
{
    public interface IPaymentService
    {
        public Task<Result<PaymentDto>> ProcessPaymentAsync(
            string orderId,
            string providerName,
            decimal amount,
            string currency);
    }
}
