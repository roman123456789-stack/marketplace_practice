using marketplace_practice.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace marketplace_practice.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.ToTable("categories");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id).HasColumnName("id");
            builder.Property(c => c.ParentCategoryId).HasColumnName("parent_category_id").IsRequired(false);
            builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            builder.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");
            builder.Property(c => c.IsActive).HasColumnName("is_active").IsRequired();

            builder.HasMany(c => c.Subcategories)
                .WithOne(s => s.ParentCategory)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
