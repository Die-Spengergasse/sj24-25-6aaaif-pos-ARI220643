using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SPG_Fachtheorie.Aufgabe1.Infrastructure;

namespace SPG_Fachtheorie.Aufgabe3.Test
{
    public class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Entferne den existierenden DbContext
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppointmentContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Füge einen neuen DbContext mit In-Memory-Datenbank hinzu
                services.AddDbContext<AppointmentContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });

                // Erstelle einen ServiceProvider und initialisiere die Datenbank
                var sp = services.BuildServiceProvider();
                using (var scope = sp.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<AppointmentContext>();
                    db.Database.EnsureCreated();
                    db.Seed();
                }
            });
        }
    }
} 