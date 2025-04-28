using Microsoft.EntityFrameworkCore;
using SPG_Fachtheorie.Aufgabe1.Commands;
using SPG_Fachtheorie.Aufgabe1.Model;
using SPG_Fachtheorie.Aufgabe1.Infrastructure;
using SPG_Fachtheorie.Aufgabe1.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SPG_Fachtheorie.Aufgabe1.Test
{
    [Collection("Sequential")] // Falls Tests nicht parallel laufen sollen (wegen DB-Zugriff)
    public class PaymentServiceTests
    {
        private AppointmentContext GetEmptyDbContext()
        {
            var options = new DbContextOptionsBuilder<AppointmentContext>()
                .UseSqlite(@"Data Source=cash_test.db") // Separate Test-DB verwenden
                .Options;

            var db = new AppointmentContext(options);
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
            // Seed Basisdaten
            db.CashDesks.Add(new CashDesk(1));
            db.Employees.Add(new Manager(1001, "Manager", "Test", new DateOnly(2000, 1, 1), null, null, "TestCar"));
            db.Employees.Add(new Cashier(1002, "Cashier", "Test", new DateOnly(2001, 1, 1), null, null, "None"));
            db.SaveChanges();
            return db;
        }

        [Theory]
        [InlineData(1001, 99, "Cash", "Invalid cash desk")]        // Ungültige CashDeskNumber
        [InlineData(999, 1, "Cash", "Invalid employee")]          // Ungültige EmployeeRegistrationNumber
        [InlineData(1001, 1, "InvalidType", "Invalid payment type")] // Ungültiger PaymentType String
        [InlineData(1002, 1, "CreditCard", "Insufficient rights to create a credit card payment.")] // Cashier versucht CreditCard
        public void CreatePaymentExceptionsTest(int employeeRegNumber, int cashDeskNumber, string paymentType, string expectedErrorMessage)
        {
            // ARRANGE
            using var db = GetEmptyDbContext();
            var service = new PaymentService(db);
            var command = new NewPaymentCommand(cashDeskNumber, paymentType, employeeRegNumber);

            // ACT & ASSERT
            var ex = Assert.Throws<PaymentServiceException>(() => service.CreatePayment(command)); // Synchron
            Assert.Equal(expectedErrorMessage, ex.Message);
        }

        [Fact]
        public void CreatePaymentOpenPaymentExceptionTest()
        {
            // ARRANGE
            using var db = GetEmptyDbContext();
            var service = new PaymentService(db);
            // Erstelle ein erstes (offenes) Payment an Kasse 1
            var cmd1 = new NewPaymentCommand(1, "Cash", 1002);
            service.CreatePayment(cmd1);

            // Versuche ein zweites Payment an der gleichen Kasse zu erstellen
            var cmd2 = new NewPaymentCommand(1, "Maestro", 1001);

            // ACT & ASSERT
            var ex = Assert.Throws<PaymentServiceException>(() => service.CreatePayment(cmd2));
            Assert.Equal("Open payment for cashdesk", ex.Message);
        }

        [Fact]
        public void CreatePaymentSuccessTest()
        {
            // ARRANGE
            using var db = GetEmptyDbContext();
            var service = new PaymentService(db);
            int employeeRegNumber = 1001;
            int cashDeskNumber = 1;
            var command = new NewPaymentCommand(cashDeskNumber, "CreditCard", employeeRegNumber); // Manager darf CreditCard

            // ACT
            var createdPayment = service.CreatePayment(command); // Gibt Payment zurück

            // ASSERT
            Assert.NotNull(createdPayment);
            Assert.True(createdPayment.Id > 0); // Id sollte vom DB generiert sein

            db.ChangeTracker.Clear();
            var paymentFromDb = db.Payments
                                  .Include(p => p.Employee)
                                  .Include(p => p.CashDesk)
                                  .FirstOrDefault(p => p.Id == createdPayment.Id);

            Assert.NotNull(paymentFromDb);
            Assert.Equal(cashDeskNumber, paymentFromDb.CashDesk.Number); // CashDesk hat Number
            Assert.Equal(employeeRegNumber, paymentFromDb.Employee.RegistrationNumber);
            Assert.Equal(PaymentType.CreditCard, paymentFromDb.PaymentType); // Enum-Vergleich
            Assert.Null(paymentFromDb.Confirmed); // Neu erstellte Payments haben Confirmed = null
            Assert.Empty(paymentFromDb.PaymentItems); // Items statt Items
        }

        [Fact]
        public void ConfirmPaymentNotFoundTest()
        {
            // ARRANGE
            using var db = GetEmptyDbContext();
            var service = new PaymentService(db);
            var nonExistentPaymentId = 999;

            // ACT & ASSERT
            var ex = Assert.Throws<PaymentServiceException>(() => service.ConfirmPayment(nonExistentPaymentId));
            Assert.Equal("Payment not found", ex.Message);
            Assert.True(ex.NotFoundException); // Prüfe spezielles Flag
        }

        [Fact]
        public void ConfirmPaymentSuccessTest()
        {
            // ARRANGE
            using var db = GetEmptyDbContext();
            var service = new PaymentService(db);
            // Erstelle ein Payment zum Bestätigen
            var payment = service.CreatePayment(new NewPaymentCommand(1, "Cash", 1002));
            var paymentId = payment.Id;

            // ACT
            service.ConfirmPayment(paymentId);

            // ASSERT
            db.ChangeTracker.Clear();
            var paymentFromDb = db.Payments.Find(paymentId);
            Assert.NotNull(paymentFromDb);
            Assert.NotNull(paymentFromDb.Confirmed); // Prüfen, ob Datum gesetzt ist
            Assert.True(paymentFromDb.Confirmed.Value.Date == DateTime.UtcNow.Date); // Ungefährer Zeitvergleich
        }

        [Fact]
        public void AddPaymentItemPaymentNotFoundTest()
        {
            // ARRANGE
            using var db = GetEmptyDbContext();
            var service = new PaymentService(db);
            var nonExistentPaymentId = 999;
            // Korrektes Command Objekt verwenden
            var itemCmd = new NewPaymentItemCommand("TestItem", 1, 10.0M, nonExistentPaymentId);

            // ACT & ASSERT
            var ex = Assert.Throws<PaymentServiceException>(() => service.AddPaymentItem(itemCmd));
            Assert.Equal("Payment not found", ex.Message);
        }

        [Fact]
        public void AddPaymentItemPaymentConfirmedTest()
        {
            // ARRANGE
            using var db = GetEmptyDbContext();
            var service = new PaymentService(db);
            // Erstelle und bestätige ein Payment
            var payment = service.CreatePayment(new NewPaymentCommand(1, "Maestro", 1001));
            service.ConfirmPayment(payment.Id); // Bestätigen über Service
            db.SaveChanges(); // Sicherstellen, dass die Änderung in der DB ist
            var itemCmd = new NewPaymentItemCommand("TestItem", 1, 10.0M, payment.Id);

            // ACT & ASSERT
            // Service wirft Exception, wenn Payment bestätigt ist
            var ex = Assert.Throws<PaymentServiceException>(() => service.AddPaymentItem(itemCmd));
            Assert.Equal("Payment already confirmed.", ex.Message); // Angepasste Meldung
        }

        [Fact]
        public void DeletePaymentNotFoundTest()
        {
            // ARRANGE
            using var db = GetEmptyDbContext();
            var service = new PaymentService(db);
            var nonExistentPaymentId = 999;

            // ACT & ASSERT
            // Service wirft KEINE Exception, sondern kehrt einfach zurück.
            // Daher können wir hier nichts mit Assert.Throws prüfen.
            // Wir rufen die Methode einfach auf.
            service.DeletePayment(nonExistentPaymentId, true);
            service.DeletePayment(nonExistentPaymentId, false);

            // Optional: Prüfen, ob sich nichts in der DB geändert hat (falls nötig)
            Assert.False(db.Payments.Any(p => p.Id == nonExistentPaymentId));
        }

        [Fact]
        public void DeletePaymentSuccessWithItemsDeletedTest() // Früher deleteItems: true
        {
            // ARRANGE
            using var db = GetEmptyDbContext();
            var service = new PaymentService(db);
            // Erstelle Payment mit Items
            var payment = service.CreatePayment(new NewPaymentCommand(1, "Cash", 1002));
            service.AddPaymentItem(new NewPaymentItemCommand("Item1", 1, 10M, payment.Id));
            service.AddPaymentItem(new NewPaymentItemCommand("Item2", 2, 5M, payment.Id));
            db.SaveChanges();
            var paymentId = payment.Id;
            var itemIds = db.PaymentItems.Where(pi => pi.Payment.Id == paymentId).Select(pi => pi.Id).ToList();
            Assert.Equal(2, itemIds.Count);

            // ACT
            service.DeletePayment(paymentId, true); // deleteItems = true

            // ASSERT
            db.ChangeTracker.Clear();
            var paymentFromDb = db.Payments.Find(paymentId);
            var itemsFromDb = db.PaymentItems.Where(pi => itemIds.Contains(pi.Id)).ToList();

            Assert.Null(paymentFromDb); // Payment sollte gelöscht sein
            Assert.Empty(itemsFromDb); // Zugehörige Items sollten gelöscht sein
        }

        [Fact]
        public void DeletePaymentThrowsExceptionWhenItemsNotDeletedTest() // Früher deleteItems: false
        {
            // ARRANGE
            using var db = GetEmptyDbContext();
            var service = new PaymentService(db);
            // Erstelle Payment mit Items
            var payment = service.CreatePayment(new NewPaymentCommand(1, "Cash", 1002));
            service.AddPaymentItem(new NewPaymentItemCommand("Item1", 1, 10M, payment.Id));
            service.AddPaymentItem(new NewPaymentItemCommand("Item2", 2, 5M, payment.Id));
            db.SaveChanges();
            var paymentId = payment.Id;

            // ACT & ASSERT
            // Erwarte eine PaymentServiceException (die die DbUpdateException kapselt)
            var ex = Assert.Throws<PaymentServiceException>(() => service.DeletePayment(paymentId, false)); // deleteItems = false

            // Optional: Genauere Prüfung der Exception-Nachricht, falls bekannt
            // Assert.Contains("FOREIGN KEY constraint failed", ex.Message); // Oder ähnlich, je nach DB und EF Core Version

            // Stelle sicher, dass Payment und Items noch existieren
            db.ChangeTracker.Clear();
            var paymentFromDb = db.Payments.Find(paymentId);
            var itemsFromDbCount = db.PaymentItems.Count(pi => pi.Payment.Id == paymentId);
            Assert.NotNull(paymentFromDb); // Payment sollte noch existieren
            Assert.Equal(2, itemsFromDbCount); // Items sollten noch existieren
        }
    }
} 