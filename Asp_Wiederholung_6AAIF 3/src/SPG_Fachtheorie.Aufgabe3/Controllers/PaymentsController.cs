using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPG_Fachtheorie.Aufgabe3.Infrastructure;
using SPG_Fachtheorie.Aufgabe3.Models;
using SPG_Fachtheorie.Aufgabe3.Commands;
using SPG_Fachtheorie.Aufgabe3.Dtos;
using SPG_Fachtheorie.Aufgabe3.Services;
using SPG_Fachtheorie.Aufgabe1.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SPG_Fachtheorie.Aufgabe3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly PaymentService _service;

        public PaymentsController(PaymentService service)
        {
            _service = service;
        }

        /// <summary>
        /// GET /api/payments
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<List<PaymentDto>> GetAllPayments(
            [FromQuery] int? cashDesk, [FromQuery] DateTime? dateFrom)
        {
            var payments = _service.Payments
                .Where(p =>
                    cashDesk.HasValue
                        ? p.CashDesk.Number == cashDesk.Value : true)
                .Where(p =>
                    dateFrom.HasValue
                        ? p.PaymentDateTime >= dateFrom.Value : true)
                .Select(p => new PaymentDto(
                    p.Id, p.Employee.FirstName, p.Employee.LastName,
                    p.PaymentDateTime,
                    p.CashDesk.Number, p.PaymentType.ToString(),
                    p.PaymentItems.Sum(pi => pi.Amount)))
                .ToList();
            return Ok(payments);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<PaymentDetailDto> GetPaymentDetail(int id)
        {
            var payment = _service.Payments
                .Where(p => p.Id == id)
                .Select(p => new PaymentDetailDto(
                    p.Id, p.Employee.FirstName, p.Employee.LastName,
                    p.CashDesk.Number, p.PaymentType.ToString(),
                    p.PaymentItems
                        .Select(pi => new PaymentItemDto(
                            pi.ArticleName, pi.Amount, pi.Price))
                        .ToList()
                    ))
                .FirstOrDefault();
            if (payment is null)
                return NotFound();
            return Ok(payment);
        }

        /// <summary>
        /// POST /api/payments
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult AddPayment([FromBody] NewPaymentCommand cmd)
        {
            try
            {
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

                cmd.CashDesk = cashDesk;
                cmd.Employee = employee;

                var payment = _service.CreatePayment(cmd);
                return CreatedAtAction(nameof(GetPaymentDetail), new { id = payment.Id }, payment);
            }
            catch (PaymentServiceException e)
            {
                return Problem(e.Message, statusCode: 400);
            }
        }

        /// <summary>
        /// DELETE /api/payments/{id}?deleteItems=true|false
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult DeletePayment(int id, [FromQuery] bool deleteItems = false)
        {
            try
            {
                _service.DeletePayment(id, deleteItems);
                return NoContent();
            }
            catch (PaymentServiceException e)
            {
                return Problem(e.Message, statusCode: 400);
            }
        }

        [HttpPatch("{id}/confirm")]
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
                if (e.Message.Contains("not found"))
                {
                    return NotFound(new ProblemDetails
                    {
                        Status = 404,
                        Title = e.Message
                    });
                }
                return Problem(e.Message, statusCode: 400);
            }
        }

        [HttpPost("{id}/items")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult AddPaymentItem(int id, [FromBody] NewPaymentItemCommand cmd)
        {
            if (id != cmd.PaymentId)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = 400,
                    Title = "PaymentId in URL and body must match"
                });
            }

            try
            {
                _service.AddPaymentItem(cmd);
                return CreatedAtAction(nameof(GetPaymentDetail), new { id }, null);
            }
            catch (PaymentServiceException e)
            {
                if (e.Message.Contains("not found"))
                {
                    return NotFound(new ProblemDetails
                    {
                        Status = 404,
                        Title = e.Message
                    });
                }
                return Problem(e.Message, statusCode: 400);
            }
        }
    }
}
