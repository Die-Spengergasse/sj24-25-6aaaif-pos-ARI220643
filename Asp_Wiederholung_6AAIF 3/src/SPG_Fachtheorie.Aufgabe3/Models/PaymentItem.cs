using System;
using System.ComponentModel.DataAnnotations;

namespace SPG_Fachtheorie.Aufgabe3.Models
{
    public class PaymentItem
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string ArticleName { get; set; } = string.Empty;
        
        [Required]
        [Range(1, int.MaxValue)]
        public int Amount { get; set; }
        
        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }
        
        [Required]
        public StorePayment Payment { get; set; } = null!;
        
        public int PaymentId { get; set; }
        
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
} 