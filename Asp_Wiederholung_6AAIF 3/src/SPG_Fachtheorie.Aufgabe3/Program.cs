using Microsoft.EntityFrameworkCore;
using SPG_Fachtheorie.Aufgabe3.Infrastructure;
using SPG_Fachtheorie.Aufgabe3.Services;
using SPG_Fachtheorie.Aufgabe1.Infrastructure;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Suche im Programmcode nach allen Klassen mit [ApiController]
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// SERVICE PROVIDER
// Stellt konfigurierte Instanzen von Klassen bereit
builder.Services.AddDbContext<AppointmentContext>(opt =>
{
    opt.UseSqlite("DataSource=cash.db");
});

// Register PaymentService
builder.Services.AddScoped<PaymentService>();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    options.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();   // Wird mit http zugegriffen, wird auf https weitergeleitet.

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        using (var db = scope.ServiceProvider.GetRequiredService<AppointmentContext>())
        {
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
            db.Seed();  // Hier fügen wir die Testdaten hinzu
        }
    }
}

// Request pipeline
app.MapControllers();  // Passt ein Controller zur Adresse? Ja: Diesen ausführen.
app.Run();

