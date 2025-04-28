using System.ComponentModel.DataAnnotations;

namespace SPG_Fachtheorie.Aufgabe3.Commands
{
    public class NewPaymentItemCommand
    {
        [Required(ErrorMessage = "ArticleName ist erforderlich")]
        [StringLength(100, ErrorMessage = "ArticleName darf nicht länger als 100 Zeichen sein")]
        public string ArticleName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Amount ist erforderlich")]
        [Range(1, int.MaxValue, ErrorMessage = "Amount muss größer als 0 sein")]
        public int Amount { get; set; }

        [Required(ErrorMessage = "Price ist erforderlich")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price muss größer als 0 sein")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "PaymentId ist erforderlich")]
        public int PaymentId { get; set; }
    }
} 