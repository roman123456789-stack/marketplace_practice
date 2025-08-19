using marketplace_practice.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace marketplace_practice.Configurations
{
    public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.ToTable("order_items");
            builder.HasKey(oi => oi.Id);
            builder.Property(oi => oi.Id).HasColumnName("id");
            builder.Property(oi => oi.OrderId).HasColumnName("order_id").IsRequired();
            builder.Property(oi => oi.CartItemId).HasColumnName("cart_item_id").IsRequired();
            builder.Property(oi => oi.Quantity).HasColumnName("quantity").IsRequired();
            builder.Property(oi => oi.Currency).HasColumnName("currency").HasConversion<string>().HasMaxLength(100).IsRequired();
            builder.Property(oi => oi.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(oi => oi.UpdatedAt).HasColumnName("updated_at");

            builder.HasOne(oi => oi.Order)
                  .WithMany(o => o.OrderItems)
                  .HasForeignKey(oi => oi.OrderId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .IsRequired();

            builder.HasOne(oi => oi.CartItem)
                  .WithOne(ci => ci.OrderItem)
                  .HasForeignKey<OrderItem>(oi => oi.CartItemId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired();
        }
    }
}
