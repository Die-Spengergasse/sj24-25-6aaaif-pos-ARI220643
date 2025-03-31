using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SPG_Fachtheorie.Aufgabe3.Models;
using SPG_Fachtheorie.Aufgabe3.Commands;

namespace SPG_Fachtheorie.Aufgabe3.Services
{
    public class PaymentServiceException : Exception
    {
        public PaymentServiceException(string message) : base(message) { }
    }

    public class PaymentService
    {
        private readonly AppointmentContext _context;

        public IQueryable<Payment> Payments => _context.Payments;

        public PaymentService(AppointmentContext context)
        {
            _context = context;
        }

        public Payment CreatePayment(NewPaymentCommand cmd)
        {
            var existingOpenPayment = _context.Payments
                .Any(p => p.CashDesk.Id == cmd.CashDeskId && !p.Confirmed.HasValue);

            if (existingOpenPayment)
            {
                throw new PaymentServiceException("Open payment for cashdesk.");
            }

            if (cmd.PaymentType == PaymentType.CreditCard && 
                cmd.Employee.Type != "Manager")
            {
                throw new PaymentServiceException("Insufficient rights to create a credit card payment.");
            }

            var payment = new Payment
            {
                PaymentDateTime = DateTime.UtcNow,
                CashDesk = _context.CashDesks.Find(cmd.CashDeskId),
                Employee = _context.Employees.Find(cmd.EmployeeId),
                PaymentType = cmd.PaymentType
            };

            _context.Payments.Add(payment);
            _context.SaveChanges();
            return payment;
        }

        public void ConfirmPayment(int paymentId)
        {
            var payment = _context.Payments.Find(paymentId);
            if (payment == null)
            {
                throw new PaymentServiceException("Payment not found.");
            }

            if (payment.Confirmed.HasValue)
            {
                throw new PaymentServiceException("Payment already confirmed.");
            }

            payment.Confirmed = DateTime.UtcNow;
            _context.SaveChanges();
        }

        public void AddPaymentItem(NewPaymentItemCommand cmd)
        {
            var payment = _context.Payments.Find(cmd.PaymentId);
            if (payment == null)
            {
                throw new PaymentServiceException("Payment not found.");
            }

            if (payment.Confirmed.HasValue)
            {
                throw new PaymentServiceException("Payment already confirmed.");
            }

            var paymentItem = new PaymentItem
            {
                ArticleName = cmd.ArticleName,
                Amount = cmd.Amount,
                Price = cmd.Price,
                Payment = payment
            };

            _context.PaymentItems.Add(paymentItem);
            _context.SaveChanges();
        }

        public void DeletePayment(int paymentId, bool deleteItems)
        {
            var payment = _context.Payments
                .Include(p => p.PaymentItems)
                .FirstOrDefault(p => p.Id == paymentId);

            if (payment == null)
            {
                throw new PaymentServiceException("Payment not found.");
            }

            if (deleteItems && payment.PaymentItems.Any())
            {
                _context.PaymentItems.RemoveRange(payment.PaymentItems);
            }
            else if (payment.PaymentItems.Any())
            {
                throw new PaymentServiceException("Payment has items. Set deleteItems to true to delete them as well.");
            }

            _context.Payments.Remove(payment);
            _context.SaveChanges();
        }
    }
} 