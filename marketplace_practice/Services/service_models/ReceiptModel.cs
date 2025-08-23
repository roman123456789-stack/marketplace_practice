using System.ComponentModel.DataAnnotations;

namespace marketplace_practice.Services.service_models
{
    public class ReceiptItem
    {
        [Required]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal UnitPrice { get; set; }

        public decimal Total => Quantity * UnitPrice;
    }

    public class ReceiptModel
    {
        [Required]
        public string StoreName { get; set; } = "Marketplace Practice";

        [Required]
        public string ReceiptNumber { get; set; } = string.Empty;

        [Required]
        public DateTime IssueDate { get; set; } = DateTime.Now;

        [Required]
        public string CustomerName { get; set; } = string.Empty;

        public List<ReceiptItem> Items { get; set; } = new();

        public decimal TotalAmount => Items.Sum(i => i.Total);
    }
}