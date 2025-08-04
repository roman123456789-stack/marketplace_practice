using marketplace_practice.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace marketplace_practice.Configurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable("payments");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).HasColumnName("id");
            builder.Property(p => p.OrderId).HasColumnName("order_id").IsRequired();
            builder.Property(p => p.ProviderName).HasColumnName("provider_name").HasMaxLength(100).IsRequired();
            builder.Property(p => p.ProviderPaymentId).HasColumnName("provider_payment_id").HasMaxLength(100).IsRequired();
            builder.Property(p => p.Amount).HasColumnName("amount").IsRequired();
            builder.Property(p => p.Currency).HasColumnName("currency").HasConversion<string>().HasMaxLength(100).IsRequired();
            builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();

            builder.HasOne(p => p.Order)
                .WithOne(o => o.Payment)
                .HasForeignKey<Payment>(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
