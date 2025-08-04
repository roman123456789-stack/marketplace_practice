using marketplace_practice.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace marketplace_practice.Configurations
{
    public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
    {
        public void Configure(EntityTypeBuilder<ProductImage> builder)
        {
            builder.ToTable("product_images");
            builder.HasKey(pi => pi.Id);
            builder.Property(pi => pi.Id).HasColumnName("id");
            builder.Property(pi => pi.ProductId).HasColumnName("product_id").IsRequired();
            builder.Property(pi => pi.Url).HasColumnName("url").HasMaxLength(300).IsRequired();
            builder.Property(pi => pi.IsMain).HasColumnName("is_main").IsRequired();
            builder.Property(pi => pi.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(pi => pi.UpdatedAt).HasColumnName("updated_at");

            builder.HasOne(pi => pi.Product)
                  .WithMany(p => p.ProductImages)
                  .HasForeignKey(pi => pi.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
