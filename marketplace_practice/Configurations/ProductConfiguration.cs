using marketplace_practice.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace marketplace_practice.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("products");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).HasColumnName("id");
            builder.Property(p => p.UserId).HasColumnName("user_id").IsRequired();
            builder.Property(p => p.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
            builder.Property(p => p.Description).HasColumnName("description");
            builder.Property(p => p.Price).HasColumnName("price").HasColumnType("money").IsRequired();
            builder.Property(p => p.PromotionalPrice).HasColumnName("promotional_price").HasColumnType("money");
            builder.Property(p => p.Size).HasColumnName("size");
            builder.Property(p => p.StockQuantity).HasColumnName("stock_quantity").IsRequired();
            builder.Property(p => p.Currency).HasColumnName("currency").HasConversion<string>().HasMaxLength(100).IsRequired();
            builder.Property(p => p.IsActive).HasColumnName("is_active").IsRequired();
            builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");

            builder.HasOne(p => p.User)
                .WithMany(u => u.Products)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            builder.HasMany(p => p.Categories)
                .WithMany(c => c.Products)
                .UsingEntity<Dictionary<string, object>>(
                    "product_categories",
                    j => j.HasOne<Category>().WithMany()
                        .HasForeignKey("category_id")
                        .OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<Product>().WithMany()
                        .HasForeignKey("product_id")
                        .OnDelete(DeleteBehavior.Cascade),
                    j => j.HasKey("category_id", "product_id")
                );
        }
    }
}
