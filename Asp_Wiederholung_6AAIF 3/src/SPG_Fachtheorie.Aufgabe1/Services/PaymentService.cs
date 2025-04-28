using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SPG_Fachtheorie.Aufgabe1.Model;
using SPG_Fachtheorie.Aufgabe1.Infrastructure;

namespace SPG_Fachtheorie.Aufgabe1.Services
{
    public class PaymentServiceException : Exception
    {
        public PaymentServiceException(string message) : base(message) { }
    }

    public class PaymentService
    {
        private readonly AppointmentContext _context;

        public IQueryable<Payment> Payments => _context.Payments
            .Include(p => p.PaymentItems)
            .Include(p => p.CashDesk)
            .Include(p => p.Employee)
            .AsQueryable();

        public PaymentService(AppointmentContext context)
        {
            _context = context;
        }

        public CashDesk? GetCashDesk(int number)
        {
            return _context.CashDesks.FirstOrDefault(c => c.Number == number);
        }

        public Employee? GetEmployee(int registrationNumber)
        {
            return _context.Employees.FirstOrDefault(e => e.RegistrationNumber == registrationNumber);
        }

        public Payment CreatePayment(CashDesk cashDesk, Employee employee, PaymentType paymentType)
        {
            if (cashDesk == null)
                throw new PaymentServiceException("CashDesk not found.");
            if (employee == null)
                throw new PaymentServiceException("Employee not found.");

            var existingOpenPayment = _context.Payments
                .Any(p => p.CashDesk.Number == cashDesk.Number && p.PaymentDateTime == default);

            if (existingOpenPayment)
            {
                throw new PaymentServiceException("Open payment for cashdesk.");
            }

            if (paymentType == PaymentType.CreditCard && 
                employee.Type != "Manager")
            {
                throw new PaymentServiceException("Insufficient rights to create a credit card payment.");
            }

            var payment = new Payment(
                cashDesk,
                DateTime.UtcNow,
                employee,
                paymentType);

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

            if (payment.PaymentDateTime != default)
            {
                throw new PaymentServiceException("Payment already confirmed.");
            }

            payment.PaymentDateTime = DateTime.UtcNow;
            _context.SaveChanges();
        }

        public void AddPaymentItem(int paymentId, string articleName, int amount, decimal price)
        {
            var payment = _context.Payments.Find(paymentId);
            if (payment == null)
            {
                throw new PaymentServiceException("Payment not found.");
            }

            if (payment.PaymentDateTime != default)
            {
                throw new PaymentServiceException("Payment already confirmed.");
            }

            var paymentItem = new PaymentItem(
                articleName,
                amount,
                price,
                payment);

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