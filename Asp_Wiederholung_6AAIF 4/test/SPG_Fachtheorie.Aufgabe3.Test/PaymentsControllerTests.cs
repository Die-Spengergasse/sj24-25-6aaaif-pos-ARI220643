using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SPG_Fachtheorie.Aufgabe1.Model;
using Xunit;

namespace SPG_Fachtheorie.Aufgabe3.Test
{
    public class PaymentsControllerTests : IClassFixture<TestWebApplicationFactory>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public PaymentsControllerTests(TestWebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Theory]
        [InlineData(1, null, 1)] // GET /api/payments?cashDesk=1
        [InlineData(null, "2024-05-13", 1)] // GET /api/payments?dateFrom=2024-05-13
        [InlineData(1, "2024-05-13", 1)] // GET /api/payments?dateFrom=2024-05-13&cashDesk=1
        public async Task GetPayments_WithFilters_ReturnsFilteredPayments(int? cashDesk, string? dateFrom, int expectedCount)
        {
            // Arrange
            var query = new List<string>();
            if (cashDesk.HasValue)
                query.Add($"cashDesk={cashDesk}");
            if (dateFrom != null)
                query.Add($"dateFrom={dateFrom}");
            var url = $"/api/payments?{string.Join("&", query)}";

            // Act
            var response = await _client.GetAsync(url);
            var payments = await response.Content.ReadFromJsonAsync<List<Payment>>();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(payments);
            Assert.Equal(expectedCount, payments.Count);
            
            // Prüfe, ob alle Payments den Filtern entsprechen
            if (cashDesk.HasValue)
                Assert.True(payments.All(p => p.CashDesk.Number == cashDesk));
            if (dateFrom != null)
            {
                var date = DateTime.Parse(dateFrom);
                Assert.True(payments.All(p => p.PaymentDateTime.Date >= date.Date));
            }
        }

        [Theory]
        [InlineData(1, HttpStatusCode.OK)] // Existierendes Payment
        [InlineData(999, HttpStatusCode.NotFound)] // Nicht existierendes Payment
        public async Task GetPaymentById_ReturnsCorrectStatusCode(int id, HttpStatusCode expectedStatusCode)
        {
            // Act
            var response = await _client.GetAsync($"/api/payments/{id}");

            // Assert
            Assert.Equal(expectedStatusCode, response.StatusCode);
        }

        [Theory]
        [InlineData(1, HttpStatusCode.OK)] // Erfolgreiche Bestätigung
        [InlineData(999, HttpStatusCode.NotFound)] // Nicht existierendes Payment
        [InlineData(2, HttpStatusCode.BadRequest)] // Bereits bestätigtes Payment
        public async Task PatchPayment_ReturnsCorrectStatusCode(int id, HttpStatusCode expectedStatusCode)
        {
            // Act
            var response = await _client.PatchAsync($"/api/payments/{id}", null);

            // Assert
            Assert.Equal(expectedStatusCode, response.StatusCode);
        }

        [Theory]
        [InlineData(1, HttpStatusCode.NoContent)] // Erfolgreiches Löschen
        [InlineData(999, HttpStatusCode.NotFound)] // Nicht existierendes Payment
        public async Task DeletePayment_ReturnsCorrectStatusCode(int id, HttpStatusCode expectedStatusCode)
        {
            // Act
            var response = await _client.DeleteAsync($"/api/payments/{id}");

            // Assert
            Assert.Equal(expectedStatusCode, response.StatusCode);
        }
    }
} 