using System.ComponentModel.DataAnnotations;

namespace marketplace_practice.Models.Enums
{
    public enum OrderStatus
    {
        [Display(Name = "Новый")]
        New = 0,

        [Display(Name = "В обработке")]
        Processing = 1,

        [Display(Name = "Подтвержден")]
        Confirmed = 2,

        [Display(Name = "Оплачен")]
        Paid = 3,

        [Display(Name = "В пути")]
        Shipped = 4,

        [Display(Name = "Доставлен")]
        Delivered = 5,

        [Display(Name = "Завершен")]
        Completed = 6,

        [Display(Name = "Отменен")]
        Cancelled = 7,

        [Display(Name = "Возврат")]
        Refunded = 8,

        [Display(Name = "На удержании")]
        OnHold = 9
    }
}
