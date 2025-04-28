using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using SPG_Fachtheorie.Aufgabe1.Model;

namespace SPG_Fachtheorie.Aufgabe3.Commands
{
    public class NewPaymentCommand : IValidatableObject
    {
        [Required(ErrorMessage = "CashDeskNumber ist erforderlich")]
        [Range(1, int.MaxValue, ErrorMessage = "Ungültige CashDesk-Nummer")]
        public int CashDeskNumber { get; set; }

        [Required(ErrorMessage = "PaymentType ist erforderlich")]
        public string PaymentType { get; set; } = string.Empty;

        [Required(ErrorMessage = "EmployeeRegistrationNumber ist erforderlich")]
        [Range(1, int.MaxValue, ErrorMessage = "Ungültige Employee-Registrierungsnummer")]
        public int EmployeeRegistrationNumber { get; set; }

        // Diese Properties werden vom Service gefüllt
        public Employee Employee { get; set; } = null!;
        public CashDesk CashDesk { get; set; } = null!;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Validiere PaymentType
            if (!Enum.TryParse<PaymentType>(PaymentType, true, out _))
            {
                yield return new ValidationResult(
                    "Ungültiger PaymentType. Erlaubte Werte sind: Cash, Maestro, CreditCard",
                    new[] { nameof(PaymentType) });
            }
        }
    }
}
