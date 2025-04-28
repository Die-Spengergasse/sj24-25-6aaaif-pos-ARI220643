using Microsoft.AspNetCore.Mvc;
using SPG_Fachtheorie.Aufgabe3.Services;
using SPG_Fachtheorie.Aufgabe3.Commands;
using Microsoft.AspNetCore.Http;
using System.Linq;
using SPG_Fachtheorie.Aufgabe3.Models;
using SPG_Fachtheorie.Aufgabe1.Model;
using System;

namespace SPG_Fachtheorie.Aufgabe3.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly PaymentService _service;

        public PaymentController(PaymentService service)
        {
            _service = service;
        }

        [HttpGet]
        public IActionResult GetPayments()
        {
            return Ok(_service.Payments);
        }

        [HttpGet("{id}")]
        public IActionResult GetPayment(int id)
        {
            var payment = _service.Payments.FirstOrDefault(p => p.Id == id);
            if (payment == null)
            {
                return NotFound();
            }
            return Ok(payment);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult AddPayment([FromBody] NewPaymentCommand cmd)
        {
            try
            {
                // Suche CashDesk und Employee
                var cashDesk = _service.GetCashDesk(cmd.CashDeskNumber);
                if (cashDesk == null)
                {
                    return Problem($"CashDesk mit Nummer {cmd.CashDeskNumber} wurde nicht gefunden.", 
                        statusCode: 400);
                }

                var employee = _service.GetEmployee(cmd.EmployeeRegistrationNumber);
                if (employee == null)
                {
                    return Problem($"Employee mit Registrierungsnummer {cmd.EmployeeRegistrationNumber} wurde nicht gefunden.", 
                        statusCode: 400);
                }

                // Parse PaymentType
                if (!Enum.TryParse<PaymentType>(cmd.PaymentType, true, out _))
                {
                    return Problem($"Ungültiger PaymentType: {cmd.PaymentType}. Erlaubte Werte sind: Cash, Maestro, CreditCard", 
                        statusCode: 400);
                }

                // Setze die gefundenen Objekte
                cmd.CashDesk = cashDesk;
                cmd.Employee = employee;

                var payment = _service.CreatePayment(cmd);
                return CreatedAtAction(nameof(GetPayment), new { id = payment.Id }, payment);
            }
            catch (PaymentServiceException e)
            {
                return Problem(e.Message, statusCode: 400);
            }
        }

        [HttpPatch("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult ConfirmPayment(int id)
        {
            try
            {
                _service.ConfirmPayment(id);
                return NoContent();
            }
            catch (PaymentServiceException e)
            {
                if (e.Message == "Payment not found.")
                {
                    return Problem(e.Message, statusCode: 404);
                }
                return Problem(e.Message, statusCode: 400);
            }
        }

        [HttpPost("{id}/items")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult AddPaymentItem(int id, [FromBody] NewPaymentItemCommand cmd)
        {
            if (id != cmd.PaymentId)
            {
                return BadRequest("PaymentId in URL and body must match");
            }

            try
            {
                _service.AddPaymentItem(cmd);
                return CreatedAtAction(nameof(GetPayment), new { id }, null);
            }
            catch (PaymentServiceException e)
            {
                return Problem(e.Message, statusCode: 400);
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult DeletePayment(int id, [FromQuery] bool deleteItems = false)
        {
            try
            {
                if (!deleteItems)
                {
                    var payment = _service.Payments
                        .FirstOrDefault(p => p.Id == id && p.PaymentItems.Any());
                    
                    if (payment != null)
                    {
                        return Problem("Payment has payment items.", statusCode: 400);
                    }
                }

                _service.DeletePayment(id, deleteItems);
                return NoContent();
            }
            catch (PaymentServiceException e)
            {
                return Problem(e.Message, statusCode: 400);
            }
        }
    }
} 