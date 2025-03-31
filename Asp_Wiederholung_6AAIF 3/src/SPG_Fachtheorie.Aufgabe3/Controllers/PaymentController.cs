using Microsoft.AspNetCore.Mvc;
using SPG_Fachtheorie.Aufgabe3.Services;
using SPG_Fachtheorie.Aufgabe3.Commands;
using Microsoft.AspNetCore.Http;
using System.Linq;

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