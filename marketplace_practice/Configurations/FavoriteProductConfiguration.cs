using marketplace_practice.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace marketplace_practice.Configurations
{
    public class FavoriteProductConfiguration : IEntityTypeConfiguration<FavoriteProduct>
    {
        public void Configure(EntityTypeBuilder<FavoriteProduct> builder)
        {
            builder.ToTable("favorite_products");
            builder.HasKey(fp => fp.Id);
            builder.Property(fp => fp.Id).HasColumnName("id");
            builder.Property(fp => fp.UserId).HasColumnName("user_id").IsRequired(); ;
            builder.Property(fp => fp.ProductId).HasColumnName("product_id").IsRequired(); ;
            builder.Property(fp => fp.IsFavorite).HasColumnName("is_favorite").IsRequired();
            builder.Property(fp => fp.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(fp => fp.UpdatedAt).HasColumnName("updated_at");

            builder.HasOne(fp => fp.User)
                  .WithMany(u => u.FavoriteProducts)
                  .HasForeignKey(fp => fp.UserId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .IsRequired();

            builder.HasOne(fp => fp.Product)
                  .WithMany(p => p.FavoriteProducts)
                  .HasForeignKey(fp => fp.ProductId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .IsRequired();
        }
    }
}
