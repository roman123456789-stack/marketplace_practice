using marketplace_practice.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace marketplace_practice.Configurations
{
    public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
    {
        public void Configure(EntityTypeBuilder<CartItem> builder)
        {
            builder.ToTable("cart_items");
            builder.HasKey(ci => ci.Id);
            builder.Property(ci => ci.Id).HasColumnName("id");
            builder.Property(ci => ci.CartId).HasColumnName("cart_id").IsRequired();
            builder.Property(ci => ci.ProductId).HasColumnName("product_id").IsRequired();
            builder.Property(ci => ci.Quantity).HasColumnName("quantity");
            builder.Property(ci => ci.CreatedAt).HasColumnName("created_at");
            builder.Property(ci => ci.UpdatedAt).HasColumnName("updated_at");

            builder.HasOne(ci => ci.Cart)
                .WithMany(c => c.CartItems)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            builder.HasOne(ci => ci.Product)
                .WithOne(p => p.CartItem)
                .HasForeignKey<CartItem>(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        }
    }
}
