using marketplace_practice.Models.Enums;
using marketplace_practice.Services.dto.Orders;
using marketplace_practice.Services.service_models;
using System.Security.Claims;

namespace marketplace_practice.Services.interfaces
{
    public interface IOrderService
    {
        //public Task<Result<OrderDto>> CreateOrderAsync(
        //    ClaimsPrincipal userPrincipal,
        //    Dictionary<long, int> productQuantities,
        //    Currency currency,
        //    string fullName,
        //    string phoneNumber,
        //    string country,
        //    string postalCode);

        //public Task<Result<OrderDto>> GetOrderByIdAsync(ClaimsPrincipal userPrincipal, string orderId);

        //public Task<Result<OrderDto>> UpdateOrderAsync(
        //    ClaimsPrincipal userPrincipal,
        //    string orderId,
        //    OrderStatus? status,
        //    Dictionary<long, int>? updatedItems,
        //    string? fullName,
        //    string? phoneNumber,
        //    string? country,
        //    string? postalCode);

        //public Task<Result<string>> DeleteOrderAsync(ClaimsPrincipal userPrincipal, string orderId);
    }
}
