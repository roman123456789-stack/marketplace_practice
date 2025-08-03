using System.ComponentModel.DataAnnotations;

namespace marketplace_practice.Models.Enums
{
    public enum OrderStatus
    {
        [Display(Name = "Новый")]
        New = 1,

        [Display(Name = "В обработке")]
        Processing = 2,

        [Display(Name = "Оплачен")]
        Paid = 3,

        [Display(Name = "Отправлен")]
        Shipped = 4,

        [Display(Name = "Доставлен")]
        Delivered = 5,

        [Display(Name = "Отменен")]
        Cancelled = 6,

        [Display(Name = "Возврат")]
        Refunded = 7
    }
}
