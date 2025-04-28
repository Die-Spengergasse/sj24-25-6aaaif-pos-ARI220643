using Microsoft.EntityFrameworkCore;
using SPG_Fachtheorie.Aufgabe3.Models;
using SPG_Fachtheorie.Aufgabe1.Model;

namespace SPG_Fachtheorie.Aufgabe3.Infrastructure
{
    public class StoreContext : DbContext
    {
        public DbSet<CashDesk> CashDesks => Set<CashDesk>();
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<StorePayment> Payments => Set<StorePayment>();
        public DbSet<SPG_Fachtheorie.Aufgabe3.Models.PaymentItem> PaymentItems => Set<SPG_Fachtheorie.Aufgabe3.Models.PaymentItem>();

        public StoreContext(DbContextOptions options)
            : base(options)
        { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var key in entityType.GetForeignKeys())
                    key.DeleteBehavior = DeleteBehavior.Restrict;
            }

            modelBuilder.Entity<Employee>().HasDiscriminator(e => e.Type);
            modelBuilder.Entity<StorePayment>().Property(p => p.PaymentType)
                .HasConversion<string>();
            modelBuilder.Entity<Employee>().OwnsOne(e => e.Address);
        }
    }
} 