using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace SPG_Fachtheorie.Aufgabe3.Commands
{
    public class UpdateConfirmedCommand : IValidatableObject
    {
        [Required(ErrorMessage = "Confirmed ist erforderlich")]
        public DateTime Confirmed { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Confirmed > DateTime.Now.AddMinutes(1))
            {
                yield return new ValidationResult(
                    "Confirmed darf nicht mehr als 1 Minute in der Zukunft liegen",
                    new[] { nameof(Confirmed) });
            }
        }
    }
} 