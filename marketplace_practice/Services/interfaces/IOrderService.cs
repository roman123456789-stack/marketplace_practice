using marketplace_practice.Models.Enums;
using marketplace_practice.Services.dto.Orders;
using marketplace_practice.Services.service_models;
using System.Security.Claims;

namespace marketplace_practice.Services.interfaces
{
    public interface IOrderService
    {
        public Task<Result<OrderDto>> CreateOrderAsync(
            ClaimsPrincipal userPrincipal,
            Dictionary<long, int> cartItemQuantities,
            string fullName,
            string phoneNumber,
            string country,
            string postalCode,
            string currency = "RUB");

        public Task<Result<OrderDto>> GetOrderByIdAsync(ClaimsPrincipal userPrincipal, string orderId);

        public Task<Result<ICollection<OrderDto>>> GetOrderListAsync(
            ClaimsPrincipal userPrincipal,
            string? targetUserId = null);

        public Task<Result<OrderDto>> UpdateOrderAsync(
            ClaimsPrincipal userPrincipal,
            string orderId,
            Dictionary<long, int>? updatedCartItems,
            string? fullName,
            string? phoneNumber,
            string? country,
            string? postalCode);

        public Task<Result<string>> DeleteOrderAsync(ClaimsPrincipal userPrincipal, string orderId);
    }
}
