using System.ComponentModel.DataAnnotations;

namespace marketplace_practice.Models.Enums
{
    public enum Role
    {
        [Display(Name = "Покупатель")]
        Customer = 1,

        [Display(Name = "Продавец")]
        Seller = 2
    }
}
