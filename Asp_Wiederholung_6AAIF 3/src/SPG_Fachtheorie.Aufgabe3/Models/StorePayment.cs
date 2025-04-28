using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SPG_Fachtheorie.Aufgabe1.Model;

namespace SPG_Fachtheorie.Aufgabe3.Models
{
    public class StorePayment
    {
        public int Id { get; set; }
        
        [Required]
        public DateTime Date { get; set; }
        
        public decimal TotalAmount { get; set; }
        
        public ICollection<PaymentItem> PaymentItems { get; set; } = new List<PaymentItem>();
        
        public DateTime? Confirmed { get; set; }

        [Required]
        public CashDesk CashDesk { get; set; } = null!;
        
        [Required]
        public Employee Employee { get; set; } = null!;
        
        [Required]
        public PaymentType PaymentType { get; set; }
    }
} 