using System.ComponentModel.DataAnnotations;

namespace marketplace_practice.Models.Enums
{
    public enum Currency
    {
        [Display(Name = "RUB")]
        RUB = 1,

        [Display(Name = "USD")]
        USD = 2,

        [Display(Name = "EUR")]
        EUR = 3,

        [Display(Name = "CNY")]
        CNY = 4
    }
}
