using Microsoft.EntityFrameworkCore;
using PaymentGatewayService.Models;

namespace PaymentGatewayService.Infrastructure
{
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
        {
        }

        public DbSet<Payment> Payments { get; set; }
        public DbSet<PaymentItem> PaymentItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.StripePaymentIntentId).IsUnique();
                entity.HasIndex(e => e.IdEstudiante);
                entity.HasIndex(e => e.IdPeriodo);
                entity.Property(e => e.Amount).HasPrecision(10, 2);
                entity.Property(e => e.Currency).HasMaxLength(3);
                entity.Property(e => e.Status).HasMaxLength(50);
            });

            modelBuilder.Entity<PaymentItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Payment)
                    .WithMany(p => p.PaymentItems)
                    .HasForeignKey(e => e.IdPayment)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.Property(e => e.PrecioUnitario).HasPrecision(10, 2);
                entity.Property(e => e.Subtotal).HasPrecision(10, 2);
            });
        }
    }
}
